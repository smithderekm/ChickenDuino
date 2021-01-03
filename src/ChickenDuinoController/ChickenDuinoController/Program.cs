using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

namespace ChickenDuinoController
{
    public class Program
    {
        

        #region Constants and Globals
        //PIN ASSIGNMENT
        //GPIO_D0 -> enableCoopDoorMotor
        //GPIO_D1 -> openCoopDoorMotor
        //GPIO_D2 -> closeCoopDoorMotor
        //GPIO_D3 -> doorOpenLED
        //GPIO_D4 -> doorClosedLED
        //GPIO_D5 -> feedDispenserServo

        //GPIO_A5 -> photoCell

        //timer 
        const int timerInterval = 600000; //60 seconds

        //door intervals
        const int openCoopDoorInterval = 11000; //2014-12-31:DMS removed pulley from door //27000; //27 seconds;
        const int closeCoopDoorInterval = 9000; //2014-12-31:DMS removed pulley from door //23000; //23 seconds

       

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

        //sleep interval
        const int sleepInterval = 10000; //debug mode 10sec; prod mode 10 minutes

        //log types
        const string INFO = "INFO";
        const string ERROR = "ERROR";

        //coop door state
        static int coopDoorState;
        const int DOOR_OPEN = 1;
        const int DOOR_CLOSED = 0;
        
        //servo 
        static Servo dispenserServo = new Servo(); //uses SecretLabs Servo api

        //feed dispenser interval
        const int feedDispenserInterval = 6000; //6 seconds 

        //door status LEDs
        static OutputPort doorOpenLedPin = new OutputPort(Pins.GPIO_PIN_D3, false);
        static OutputPort doorClosedLedPin = new OutputPort(Pins.GPIO_PIN_D4, false);

        //onboard button
        static InputPort buttonPin = new InputPort(Pins.ONBOARD_BTN, false, Port.ResistorMode.Disabled);

        #endregion

        #region Main
        public static void Main()
        {
            
            InitializeSystems();

            WriteLog(INFO, "Starting ChickenDuino Controller");
            coopDoorState = DOOR_CLOSED;


            while (true)
            {
                ReadPhotoCell();
                ReadButton();

                if (photocellReadingLevel == 1)
                {
                    if (photocellReadingLevel != 2)
                    {
                        if (photocellReadingLevel != 3)
                        {
                            if (coopDoorState != DOOR_CLOSED)
                            {
                                WriteLog(INFO, "Dark condition detected.  Closing Coop Door.");

                                //dark
                                CloseCoopDoor();

                                WriteLog(INFO, "Coop Door Closed");
                            }
                            else
                                WriteLog(INFO, "Door Closed; Chickens Sleeping; Shhh.");
                        }
                    }
                }

                if (photocellReadingLevel == 3)
                {
                    if (photocellReadingLevel != 2)
                    {
                        if (photocellReadingLevel != 1)
                        {
                            if (coopDoorState != DOOR_OPEN)
                            {
                                //light
                                WriteLog(INFO, "Light condition detected.  Opening Coop Door");

                                OpenCoopDoor();
                                WriteLog(INFO, "Coop Door Opened");

                                WriteLog(INFO, "Dispensing Feed");
                                DispenseFeed();
                                WriteLog(INFO, "Feed Dispense Complete");
                            }
                            else
                                WriteLog(INFO, "Door open; Chickens fed; Daytime.");
                        }
                    }
                }

                Thread.Sleep(sleepInterval);
            }
        }
        #endregion

        #region Utility Methods

        private static void InitializeSystems()
        {
            dispenserServo.Attach(Pins.GPIO_PIN_D5);

            //dispenserServo.Degree = 0;
            //Thread.Sleep(1000);
            //dispenserServo.Degree = 180;
            //Thread.Sleep(1000);
            //dispenserServo.Degree = 0;

        }

        private static void WriteLog(string messageType, string message)
        {
            Debug.Print(DateTime.Now.ToString() + ":" + messageType + " " + message);
        }

        private static void ReadPhotoCell()
        {
            WriteLog(INFO, "Reading photocell");
                        
            //2014-09-24 loop testing showed max of 1 in full light, min of 0.09 with lights out
            //2014-10-19 loop testing with soldered board and 3.3v showing light condition as lower value, dark condition approaching 1

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

        private static void ReadButton()
        {
            WriteLog(INFO, "Reading button");
            bool pinState = buttonPin.Read();


            if (pinState && coopDoorState == DOOR_OPEN)
                photocellReadingLevel = 1; //dark
            else if (pinState && coopDoorState == DOOR_CLOSED)
                photocellReadingLevel = 3;

        }
        #endregion

        #region Door

        private static void StopCoopDoorMotor()
        {
            openCoopDoorMotorPinA.Write(false);
            closeCoopDoorMotorPinA.Write(false);
            enableCoopDoorMotorPinA.Write(false);

        }

        private static void CloseCoopDoor()
        {
            if (coopDoorState != DOOR_CLOSED)
            {
                closeCoopDoorMotorPinA.Write(true);
                openCoopDoorMotorPinA.Write(false);
                enableCoopDoorMotorPinA.Write(true);

                //
                Thread.Sleep(closeCoopDoorInterval);
                StopCoopDoorMotor();

                //set LEDs
                doorOpenLedPin.Write(false);
                doorClosedLedPin.Write(true);

                coopDoorState = DOOR_CLOSED;
            }
        }

        private static void OpenCoopDoor()
        {
            if (coopDoorState != DOOR_OPEN)
            {
                closeCoopDoorMotorPinA.Write(false);
                openCoopDoorMotorPinA.Write(true);
                enableCoopDoorMotorPinA.Write(true);

                //
                Thread.Sleep(openCoopDoorInterval);
                StopCoopDoorMotor();

                //set LEDs
                doorOpenLedPin.Write(true);
                doorClosedLedPin.Write(false);

                coopDoorState = DOOR_OPEN;
            }
        }

        #endregion

        #region Feed Dispenser
        private static void DispenseFeed()
        {
            WriteLog(INFO, "Opening feed dispenser");

            //set dispenser servo to 180
            dispenserServo.Degree = 180;

            //wait while food flows
            Thread.Sleep(feedDispenserInterval);

            //set dispenser servo to 0
            //0.6ms duration per spec
            WriteLog(INFO, "Closing feed dispenser");

            dispenserServo.Degree = 0;

        }
        #endregion

    }
}
