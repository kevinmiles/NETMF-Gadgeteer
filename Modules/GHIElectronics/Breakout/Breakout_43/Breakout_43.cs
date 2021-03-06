﻿using GTI = Gadgeteer.SocketInterfaces;
using GTM = Gadgeteer.Modules;

namespace Gadgeteer.Modules.GHIElectronics {
	/// <summary>A Breakout module for Microsoft .NET Gadgeteer.</summary>
	public class Breakout : GTM.Module {
		private Socket socket;

		/// <summary>The mainboard socket which this module is plugged into.</summary>
		public Socket Socket { get { return this.socket; } }

		/// <summary>Constructs a new instance.</summary>
		/// <param name="socketNumber">The mainboard socket that has the module plugged into it.</param>
		public Breakout(int socketNumber) {
			this.socket = Socket.GetSocket(socketNumber, true, this, null);
		}

		/// <summary>Creates a digital input on the given pin.</summary>
		/// <param name="pin">The pin to create the interface on.</param>
		/// <param name="glitchFilterMode">The glitch filter mode for the interface.</param>
		/// <param name="resistorMode">The resistor mode for the interface.</param>
		/// <returns>The new interface.</returns>
		public GTI.DigitalInput CreateDigitalInput(Socket.Pin pin, GTI.GlitchFilterMode glitchFilterMode, GTI.ResistorMode resistorMode) {
			return GTI.DigitalInputFactory.Create(this.socket, pin, glitchFilterMode, resistorMode, this);
		}

		/// <summary>Creates a digital output on the given pin.</summary>
		/// <param name="pin">The pin to create the interface on.</param>
		/// <param name="initialState">The initial state for the interface.</param>
		/// <returns>The new interface.</returns>
		public GTI.DigitalOutput CreateDigitalOutput(Socket.Pin pin, bool initialState) {
			return GTI.DigitalOutputFactory.Create(this.socket, pin, initialState, this);
		}

		/// <summary>Creates a digital input/output on the given pin.</summary>
		/// <param name="pin">The pin to create the interface on.</param>
		/// <param name="initialState">The initial state for the interface.</param>
		/// <param name="glitchFilterMode">The glitch filter mode for the interface.</param>
		/// <param name="resistorMode">The resistor mode for the interface.</param>
		/// <returns>The new interface.</returns>
		public GTI.DigitalIO CreateDigitalIO(Socket.Pin pin, bool initialState, GTI.GlitchFilterMode glitchFilterMode, GTI.ResistorMode resistorMode) {
			return GTI.DigitalIOFactory.Create(this.socket, pin, initialState, glitchFilterMode, resistorMode, this);
		}

		/// <summary>Creates an interrupt input on the given pin.</summary>
		/// <param name="pin">The pin to create the interface on.</param>
		/// <param name="glitchFilterMode">The glitch filter mode for the interface.</param>
		/// <param name="resistorMode">The resistor mode for the interface.</param>
		/// <param name="interruptMode">The interrupt mode for the interface.</param>
		/// <returns>The new interface.</returns>
		public GTI.InterruptInput CreateInterruptInput(Socket.Pin pin, GTI.GlitchFilterMode glitchFilterMode, GTI.ResistorMode resistorMode, GTI.InterruptMode interruptMode) {
			return GTI.InterruptInputFactory.Create(this.socket, pin, glitchFilterMode, resistorMode, interruptMode, this);
		}

		/// <summary>Creates an analog input on the given pin.</summary>
		/// <param name="pin">The pin to create the interface on.</param>
		/// <returns>The new interface.</returns>
		public GTI.AnalogInput CreateAnalogInput(Socket.Pin pin) {
			return GTI.AnalogInputFactory.Create(this.socket, pin, this);
		}

		/// <summary>Creates an analog output on the given pin.</summary>
		/// <param name="pin">The pin to create the interface on.</param>
		/// <returns>The new interface.</returns>
		public GTI.AnalogOutput CreateAnalogOutput(Socket.Pin pin) {
			return GTI.AnalogOutputFactory.Create(this.socket, pin, this);
		}

		/// <summary>Creates a pwm output on the given pin.</summary>
		/// <param name="pin">The pin to create the interface on.</param>
		/// <returns>The new interface.</returns>
		public GTI.PwmOutput CreatePwmOutput(Socket.Pin pin) {
			return GTI.PwmOutputFactory.Create(this.socket, pin, false, this);
		}
	}
}