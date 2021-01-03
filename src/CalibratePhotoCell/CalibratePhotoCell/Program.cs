            using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;


namespace CalibratePhotoCell
{
    public class Program
    {
        //PIN ASSIGNMENT
        //GPIO_D0 -> enableCoopDoorMotor
        //GPIO_D1 -> openCoopDoorMotor
        //GPIO_D2 -> closeCoopDoorMotor
        //GPIO_D3 -> doorOpenLED
        //GPIO_D4 -> doorClosedLED
        //GPIO_D5 -> feedDispenserServo

        //GPIO_A5 -> photoCell

        //timer 
        const int timerInterval = 600000;

        //door intervals
        const int openCoopDoorInterval = 27000; //30 seconds;
        const int closeCoopDoorInterval = 23000; //30 seconds

        //feed dispenser interval
        const int feedDispenserInterval = 6000; //6 seconds

        // initialize pins
        // door control
        static OutputPort enableCoopDoorMotorPinA = new OutputPort(Pins.GPIO_PIN_D0, false);
        static OutputPort openCoopDoorMotorPinA = new OutputPort(Pins.GPIO_PIN_D1, false);
        static OutputPort closeCoopDoorMotorPinA = new OutputPort(Pins.GPIO_PIN_D2, false);

        //photocell
        static double photocellReading; //analog reading
        static double photocellReadingLevel; //indicator (light, twilight, dark)
        static AnalogInput photoCellPin = new AnalogInput(Cpu.AnalogChannel.ANALOG_5);
        const double maxVoltage = 3.3;
        const int maxAdcValue = 1023;


        //onboard button
        static InputPort buttonPin = new InputPort(Pins.ONBOARD_BTN, false, Port.ResistorMode.Disabled);

        //sleep interval
        const int sleepInterval = 10000; //debug mode 10sec; prod mode 10 minutes

        //log types
        const string INFO = "INFO";
        const string ERROR = "ERROR";


        public static void Main()
        {

            while (true)
            {
                ReadPhotoCell();

                Thread.Sleep(sleepInterval);
            }
        }


        private static void WriteLog(string messageType, string message)
        {
            Debug.Print(DateTime.Now.ToString() + ":" + messageType + " " + message);
        }

        private static void ReadPhotoCell()
        {
            WriteLog(INFO, "Reading photocell");

            //2014-09-24 loop testing showed max of 1 in full light, min of 0.09 with lights out

            photocellReading = photoCellPin.Read();

            WriteLog(INFO, "Photocell raw value: " + photocellReading.ToString());

            //may need to convert value here

            //boundaries: 1 to 0.75
            if (photocellReading >= 0.75 && photocellReading < 1)
                photocellReadingLevel = 1; //dark
            else if (photocellReading >= 0.5 && photocellReading <= 0.74)
                photocellReadingLevel = 2; //twilight
            else if (photocellReading <= 0.49)
                photocellReadingLevel = 3; //light

        }

    }
}
