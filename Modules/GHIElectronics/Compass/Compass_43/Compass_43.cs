﻿using System;
using Microsoft.SPOT;


using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using GTI = Gadgeteer.SocketInterfaces;

namespace Gadgeteer.Modules.GHIElectronics
{
    /// <summary>
    /// A Compass module for Microsoft .NET Gadgeteer, based on the Honeywell HMC5883
    /// </summary>
    public class Compass : GTM.Module
    {

        // Note: A constructor summary is auto-generated by the doc builder.
        /// <summary></summary>
        /// <param name="socketNumber">The socket that this module is plugged in to.</param>
        public Compass(int socketNumber)
        {
            // This finds the Socket instance from the user-specified socket number.  
            // This will generate user-friendly error messages if the socket is invalid.
            // If there is more than one socket on this module, then instead of "null" for the last parameter, 
            // put text that identifies the socket to the user (e.g. "S" if there is a socket type S)
            Socket socket = Socket.GetSocket(socketNumber, true, this, null);

            continuousTimer = new Gadgeteer.Timer(new TimeSpan(0, 0, 0, 0, 200));
            continuousTimer.Tick += new Timer.TickEventHandler(continuousTimer_Tick);

            i2c = GTI.I2CBusFactory.Create(socket, 0x1E, 100, this);

            //dataReady = GTI.InterruptInputFactory.Create(socket, Socket.Pin.Three, GTI.GlitchFilterMode.Off, GTI.ResistorMode.PullDown, GTI.InterruptMode.RisingEdge, this);
            dataReady = GTI.InterruptInputFactory.Create(socket, Socket.Pin.Three, GTI.GlitchFilterMode.Off, GTI.ResistorMode.Disabled, GTI.InterruptMode.RisingEdge, this);
            dataReady.Interrupt += dataReady_Interrupt;
        }

        private GTI.InterruptInput dataReady;

        private GTI.I2CBus i2c;

        // Read/write buffers
        private byte[] _readBuffer8 = new byte[1];
        private byte[] _writeBuffer8 = new byte[1];
        private byte[] _writeBuffer16 = new byte[2];
        private byte[] readBuffer48 = new byte[6];

        private GT.Timer continuousTimer;
        private bool continuousMeasurement = false;

        private enum Register : byte
        {
            // From HMC5883.pdf Datasheet, pg. 12.              
            //                          R/W    Bit 7    Bit 6    Bit 5    Bit 4    Bit 3    Bit 2    Bit 1    Bit 0   
            // ------------------------------------------------------------------------------------------------------
            CRA = 0x00,             //  R/W    
            CRB = 0x01,             //  R/W 
            MR = 0x02,              //  R/W
            DXRA = 0x03,            //  R
            DXRB = 0x04,            //  R
            DZRA = 0x05,            //  R
            DZRB = 0x06,            //  R
            DYRA = 0x07,            //  R
            DYRB = 0x08,            //  R
            SR = 0x09,             //  R
            IRA = 0x0A,             //  R
            IRB = 0x0B,             //  R
            IRC = 0x0C              //  R
        }

        /// <summary>
        /// Possible sensing gain values.
        /// </summary>
        public enum Gain : byte
        {
            /// <summary>
            /// +/- 0.88 Ga
            /// </summary>
            Gain1 = 0x00,

            /// <summary>
            /// +/- 1.2 Ga (default)
            /// </summary>
            Gain2 = 0x20,

            /// <summary>
            /// +/- 1.9 Ga
            /// </summary>
            Gain3 = 0x40,

            /// <summary>
            /// +/- 2.5 Ga
            /// </summary>
            Gain4 = 0x60,

            /// <summary>
            /// +/- 4.0 Ga
            /// </summary>
            Gain5 = 0x80,

            /// <summary>
            /// +/- 4.7 Ga
            /// </summary>
            Gain6 = 0xA0,

            /// <summary>
            /// +/- 5.6 Ga
            /// </summary>
            Gain7 = 0xC0,

            /// <summary>
            /// +/- 8.1 Ga
            /// </summary>
            Gain8 = 0xE0,

        }

        private enum Mode : byte
        {
            /// <summary>
            /// Continous-Measurement Mode.
            /// </summary>
            Continous = 0x00,

            /// <summary>
            /// Singleshot-Measurement Mode.
            /// </summary>
            SingleMode = 0x01,

            /// <summary>
            /// Idle Mode.
            /// </summary>
            IdleMode = 0x02,

            /// <summary>
            /// Sleep Mode.
            /// </summary>
            SleepMode = 0x03
        }

        private byte ReadByte(Register register)
        {
            _writeBuffer8[0] = (byte)register;
            i2c.WriteRead(_writeBuffer8, _readBuffer8);
            return _readBuffer8[0];
        }

        private void Read(Register register, byte[] readBuffer)
        {
            _writeBuffer8[0] = (byte)register;
            i2c.WriteRead(_writeBuffer8, readBuffer);
        }

        private void Write(Register register, byte value)
        {
            _writeBuffer16[0] = (byte)register;
            _writeBuffer16[1] = (byte)value;
            i2c.Write(_writeBuffer16);
        }

        /// <summary>
        /// Requests a single reading from the <see cref="Compass"/> and raises the <see cref="MeasurementComplete"/> event when complete.
        /// </summary>
        public void RequestMeasurement()
        {
            // Do a single reading. This should result on the dataReady_Interrupt being raised.
            i2c.Write(new byte[] { (byte)Register.MR, (byte)Mode.SingleMode });
        }

        // This fires when new data is ready to be read
        void dataReady_Interrupt(GTI.InterruptInput sender, bool value)
        {
            int rawX, rawY, rawZ;

            // Read all the registers at once

            Read(Register.DXRA, readBuffer48);

            rawX = (readBuffer48[0] << 8) | readBuffer48[1];
            rawZ = (readBuffer48[2] << 8) | readBuffer48[3];
            rawY = (readBuffer48[4] << 8) | readBuffer48[5];

            rawX = (((rawX >> 15) == 1) ? -32767 : 0) + (rawX & 0x7FFF);
            rawZ = (((rawZ >> 15) == 1) ? -32767 : 0) + (rawZ & 0x7FFF);
            rawY = (((rawY >> 15) == 1) ? -32767 : 0) + (rawY & 0x7FFF);

            if (rawX == -4096 || rawY == -4096 || rawZ == -4096)
            {
                DebugPrint("Invalid data read. Measurement discarded.");
                return;
            }

            double angle = Atan2((double)rawY, (double)rawX) * (180 / 3.14159265) + 180;

            SensorData sensorData = new SensorData(angle, rawX, rawY, rawZ);

            OnMeasurementCompleteEvent(this, sensorData);

        }

        /// <summary>
        /// A set of sensor measurements.
        /// </summary>
        public class SensorData
        {
            /// <summary>
            /// Raw X-axis sensor data.
            /// </summary>
            public int X { get; private set; }
            /// <summary>
            /// Raw Y-axis sensor data.
            /// </summary>
            public int Y { get; private set; }
            /// <summary>
            /// Raw Z-axis sensor data.
            /// </summary>
            public int Z { get; private set; }
            /// <summary>
            /// Angle of heading in the XY plane, in radians.
            /// </summary>
            public double Angle { get; private set; }

            /// <summary>
            /// A set of sensor measurements.
            /// </summary>
            /// <param name="angle">Angle of heading in the XY plane, in radians.</param>
            /// <param name="x">Raw X-axis sensor data.</param>
            /// <param name="y">Raw Y-axis sensor data.</param>
            /// <param name="z">Raw Z-axis sensor data.</param>            
            public SensorData(double angle, int x, int y, int z)
            {
                this.Angle = angle;
                this.X = x;
                this.Y = y;
                this.Z = z;
            }

            /// <summary>
            /// Provides a string representation of the <see cref="SensorData"/> instance.
            /// </summary>
            /// <returns>A string describing the values contained in the object.</returns>
            public override string ToString()
            {
                return "Angle: " + Angle.ToString("f2") + " X: " + X.ToString("f2") + " Y: " + Y.ToString("f2") + " Z: " + Z.ToString("f2");
            }
        }

        /// <summary>
        /// Sets the sensor gain value.
        /// </summary>
        /// <param name="gain">Gain value.</param>
        public void SetGain(Gain gain)
        {
            Write(Register.CRB, (byte)gain);
        }

        private const double sq2p1 = 2.414213562373095048802e0F;
        private const double sq2m1 = .414213562373095048802e0F;
        private const double pio4 = .785398163397448309615e0F;
        private const double pio2 = 1.570796326794896619231e0F;
        private const double atan_p4 = .161536412982230228262e2F;
        private const double atan_p3 = .26842548195503973794141e3F;
        private const double atan_p2 = .11530293515404850115428136e4F;
        private const double atan_p1 = .178040631643319697105464587e4F;
        private const double atan_p0 = .89678597403663861959987488e3F;
        private const double atan_q4 = .5895697050844462222791e2F;
        private const double atan_q3 = .536265374031215315104235e3F;
        private const double atan_q2 = .16667838148816337184521798e4F;
        private const double atan_q1 = .207933497444540981287275926e4F;
        private const double atan_q0 = .89678597403663861962481162e3F;

        /// <summary>
        /// Returns the angle whose tangent is the quotient of two specified numbers.
        /// </summary>
        /// <param name="y">The y coordinate of a point</param>
        /// <param name="x">The x coordinate of a point</param>
        /// <returns>The arctangent of x/y</returns>
        private double Atan2(double y, double x)
        {

            if ((x + y) == x)
            {
                if ((x == 0F) & (y == 0F)) return 0F;

                if (x >= 0.0F)
                    return pio2;
                else
                    return (-pio2);
            }
            else if (y < 0.0F)
            {
                if (x >= 0.0F)
                    return ((pio2 * 2) - Atans((-x) / y));
                else
                    return (((-pio2) * 2) + Atans(x / y));

            }
            else if (x > 0.0F)
            {
                return (Atans(x / y));
            }
            else
            {
                return (-Atans((-x) / y));
            }
        }

        private double Atans(double x)
        {
            if (x < sq2m1)
                return (Atanx(x));
            else if (x > sq2p1)
                return (pio2 - Atanx(1.0F / x));
            else
                return (pio4 + Atanx((x - 1.0F) / (x + 1.0F)));
        }

        private double Atanx(double x)
        {
            double argsq;
            double value;

            argsq = x * x;
            value = ((((atan_p4 * argsq + atan_p3) * argsq + atan_p2) * argsq + atan_p1) * argsq + atan_p0);
            value = value / (((((argsq + atan_q4) * argsq + atan_q3) * argsq + atan_q2) * argsq + atan_q1) * argsq + atan_q0);
            return (value * x);
        }

        /// <summary>
        /// Gets or sets the interval at which continuous measurements are taken.
        /// </summary>
        /// <remarks>
        /// The default value for this property is 200 milliseconds.
        /// </remarks>
        public TimeSpan ContinuousMeasurementInterval
        {
            get
            {
                return continuousTimer.Interval;
            }
            set
            {
                continuousTimer.Stop();
                continuousTimer.Interval = value;
                if (continuousMeasurement) continuousTimer.Start();
            }
        }

        /// <summary>
        /// Starts continuous measurements.
        /// </summary>
        /// <remarks>
        /// When this method is called, the <see cref="Compass"/> begins taking continuous measurements.
        /// At each <see cref="ContinuousMeasurementInterval"/>, it calls the <see cref="RequestMeasurement"/> method,
        /// which raises the <see cref="MeasurementComplete"/> event.
        /// </remarks>
        public void StartContinuousMeasurements()
        {
            continuousMeasurement = true;
            continuousTimer.Start();
        }

        /// <summary>
        /// Stops continuous measurements.
        /// </summary>
        public void StopContinuousMeasurements()
        {
            continuousMeasurement = false;
            continuousTimer.Stop();
        }

        void continuousTimer_Tick(Timer timer)
        {
            if (!continuousMeasurement)
            {
                timer.Stop();
                return;
            }
            RequestMeasurement();
        }

        /// <summary>
        /// Represents the delegate used for the <see cref="MeasurementComplete"/> event.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="sensorData">The <see cref="SensorData"/> object that contains the results of the reading.</param>
        public delegate void MeasurementCompleteEventHandler(Compass sender, SensorData sensorData);

        /// <summary>
        /// Event raised when a measurement reading is completed.
        /// </summary>
        public event MeasurementCompleteEventHandler MeasurementComplete;

        private MeasurementCompleteEventHandler _OnMeasurementComplete;

        /// <summary>
        /// Raises the <see cref="MeasurementComplete"/> event.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="sensorData">The <see cref="SensorData"/> object that contains the results of the measurement.</param>
        protected virtual void OnMeasurementCompleteEvent(Compass sender, SensorData sensorData)
        {
            if (_OnMeasurementComplete == null) _OnMeasurementComplete = new MeasurementCompleteEventHandler(OnMeasurementCompleteEvent);
            if (Program.CheckAndInvoke(MeasurementComplete, _OnMeasurementComplete, sender, sensorData))
            {
                MeasurementComplete(sender, sensorData);
            }
        }


    }
}
