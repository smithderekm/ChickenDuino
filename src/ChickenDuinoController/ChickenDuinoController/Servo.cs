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
    public class Servo : IDisposable
    {
        private SecretLabs.NETMF.Hardware.PWM servoPort;

        public Servo() { }

        public void Attach(Cpu.Pin servoPin)
        {
            servoPort = new SecretLabs.NETMF.Hardware.PWM(servoPin);
        }

        public int Degree { 
            set
            {
                /// Range checks
                if (value > 180)
                    value = 180;

                if (value <= 0)
                    value = 1; //did not like 0 position

                servoPort.SetPulse(20000, (uint)((value*10) + 600));

            }
        }

        public void Dispose()
        {
            servoPort.Dispose();

        }
    }
}
