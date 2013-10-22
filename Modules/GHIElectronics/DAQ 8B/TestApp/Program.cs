﻿using Microsoft.SPOT;
using System.Threading;
using GTM = Gadgeteer.Modules;

namespace TestApp
{
	public partial class Program
	{
		void ProgramStarted()
		{
			var atd = new GTM.GHIElectronics.DAQ_8B(6);
			new Thread(() =>
			{
				string result;
				while (true)
				{
					result = atd.GetReading(GTM.GHIElectronics.DAQ_8B.Channel.P1).ToString() + " ";
					result += atd.GetReading(GTM.GHIElectronics.DAQ_8B.Channel.P2).ToString() + " ";
					result += atd.GetReading(GTM.GHIElectronics.DAQ_8B.Channel.P3).ToString() + " ";
					result += atd.GetReading(GTM.GHIElectronics.DAQ_8B.Channel.P4).ToString() + " ";
					result += atd.GetReading(GTM.GHIElectronics.DAQ_8B.Channel.P5).ToString() + " ";
					result += atd.GetReading(GTM.GHIElectronics.DAQ_8B.Channel.P6).ToString() + " ";
					result += atd.GetReading(GTM.GHIElectronics.DAQ_8B.Channel.P7).ToString() + " ";
					result += atd.GetReading(GTM.GHIElectronics.DAQ_8B.Channel.P8).ToString();

					Debug.Print(result);

					Thread.Sleep(50);
				}
			}).Start();
		}
	}
}