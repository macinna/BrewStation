/*---------------------------------------------------------------------------
 * Copyright (C) 2002-2003 Dallas Semiconductor Corporation, All Rights Reserved.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included
 * in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY,  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL DALLAS SEMICONDUCTOR BE LIABLE FOR ANY CLAIM, DAMAGES
 * OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
 * ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * Except as contained in this notice, the name of Dallas Semiconductor
 * shall not be used except as stated in the Dallas Semiconductor
 * Branding Policy.
 *---------------------------------------------------------------------------
 */

// This file is distributed as part of the open source OWdotNET project.
// Project pages: https://sourceforge.net/projects/owdotnet
// Web Site:      http://owdotnet.sourceforge.net/

using System;
using System.Collections.Generic;
using System.Text;

namespace DalSemi.OneWire.Container
{



	/// <summary>
	/// Interface class for 1-Wire devices that contain Real-Time clocks.
	/// This class should be implemented for each Clock type 1-Wire device.
	/// 
	/// Features
	/// Supports clock alarm enabling and setting on devices with clock alarms
	/// Supports enabling and disabling the clock on devices that can disable their oscillators
	/// 
	/// Usage
	/// ClockContainer extends OneWireSensor, so the general usage model applies to any ClockContainer:
	///   ReadDevice()
	///   Perform operations on the ClockContainer
	///   WriteDevice(byte[])
	///   
	/// Consider this interaction with a ClockContainer that reads from the
	/// Real-Time clock, then tries to set it to the system's current clock setting before
	/// disabling the oscillator:
	/// 
	///     // clockcontainer is a com.dalsemi.onewire.container.ClockContainer
	///     byte[] state = clockcontainer.readDevice();
	///     long current_time = clockcontainer.getClock(state);
	///     System.out.println("Current time is :"+(new Date(current_time)));
	///     
	///     long system_time = System.currentTimeMillis();
	///     clockcontainer.setClock(system_time,state);
	///     clockcontainer.writeDevice(state);
	///     
	///     //now try to disable to clock oscillator
	///     if (clockcontainer.canDisableClock())
	///     {
	///          state = clockcontainer.readDevice();
	///          clockcontainer.setClockRunEnable(false,state);
	///          clockcontainer.writeDevice(state);
	///     }
	/// </summary>
	public interface ClockContainer : IOneWireSensor
	{


		//--------
		//-------- Clock Feature methods
		//--------

		/// <summary>
		/// Determines whether the clock has an alarm feature
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if present; otherwise, <c>false</c>.
		/// </returns>
		bool HasClockAlarm();

		/// <summary>
		/// Checks to see if the clock can be disabled.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if the clock can be disabled; otherwise, <c>false</c>.
		/// </returns>
		bool CanDisableClock();

		/// <summary>
		/// Gets the clock resolution in milliseconds
		/// </summary>
		/// <returns>the clock resolution in milliseconds</returns>
		long GetClockResolution();

		//--------
		//-------- Clock 'get' Methods
		//--------

		/// <summary>
		/// Extracts the Real-Time clock value in milliseconds.
		/// </summary>
		/// <param name="state">current state of the device returned from <c>ReadDevice()</c></param>
		/// <returns>the time represented in this clock in milliseconds since 1970</returns>
		long GetClock( byte[] state );

		/// <summary>
		/// Extracts the clock alarm value for the Real-Time clock.
		/// </summary>
		/// <param name="state">current state of the device returned from <c>ReadDevice()</c></param>
		/// <returns>the set value of the clock alarm in milliseconds since 1970</returns>
		/// <exception cref="OneWireException">if this device does not have clock alarms</exception>
		long GetClockAlarm( byte[] state );

		/// <summary>
		/// Checks if the clock alarm flag has been set.
		/// This will occur when the value of the Real-Time clock equals the value of the clock alarm.
		/// </summary>
		/// <param name="state">current state of the device returned from <c>ReadDevice()</c></param>
		/// <returns>
		/// 	<c>true</c> if the Real-Time clock is alarming
		/// </returns>
		bool IsClockAlarming( byte[] state );

		/// <summary>
		/// Determines whether the clock alarm is enabled.
		/// </summary>
		/// <param name="state">current state of the device returned from <c>ReadDevice()</c></param>
		/// <returns>
		/// 	<c>true</c> if clock alarm is enabled.
		/// </returns>
		bool IsClockAlarmEnabled( byte[] state );

		/// <summary>
		/// Checks if the device's oscillator is enabled.  The clock will not increment if the clock oscillator is not enabled.
		/// </summary>
		/// <param name="state">current state of the device returned from <c>ReadDevice()</c></param>
		/// <returns>
		/// 	<c>true</c> if the clock is running.
		/// </returns>
		bool IsClockRunning( byte[] state );

		//--------
		//-------- Clock 'set' Methods
		//--------

		/// <summary>
		/// Sets the Real-Time clock. The method <c>writeDevice()</c> must be called to finalize
		/// changes to the device.  Note that multiple 'set' methods can be called before one call
		/// to <c>WriteDevice()</c>.
		/// </summary>
		/// <param name="time">New value for the Real-Time clock, in milliseconds since January 1, 1970</param>
		/// <param name="state">current state of the device returned from <c>ReadDevice()</c></param>
		void SetClock( long time, byte[] state );

		/// <summary>
		/// Sets the clock alarm.
		/// The method <c>WriteDevice()</c> must be called to finalize
		/// changes to the device.  Note that multiple 'set' methods can
		/// be called before one call to <c>WriteDevice()</c>.  Also note that
		/// not all clock devices have alarms.  Check to see if this device has
		/// alarms first by calling the <code>HasClockAlarm()</code> method.
		/// </summary>
		/// <param name="time">new value for the Real-Time clock alarm, in milliseconds since January 1, 1970</param>
		/// <param name="state">current state of the device returned from <c>ReadDevice()</c></param>	
		void SetClockAlarm( long time, byte[] state );

		/// <summary>
		/// Enables or disables the oscillator, turning the clock 'on' and 'off'.
		/// The method <c>WriteDevice()</c> must be called to finalize
		/// changes to the device.  Note that multiple 'set' methods can
		/// be called before one call to <c>WriteDevice()</c>.  Also note that
		/// not all clock devices can disable their oscillators.  Check to see if this device can
		/// disable its oscillator first by calling the <c>CanDisableClock()</c> method.
		/// </summary>
		/// <param name="runEnable">true to enable the clock oscillator</param>
		/// <param name="state">current state of the device returned from <c>ReadDevice()</c></param>
		/// <exception cref="OneWireException">if the clock oscillator cannot be disabled</exception>	
		void SetClockRunEnable( bool runEnable, byte[] state );

		/// <summary>
		/// Enables or disables the clock alarm. 
		/// The method <c>WriteDevice()</c> must be called to finalize
		/// changes to the device.  Note that multiple 'set' methods can
		/// be called before one call to <c>WriteDevice()</c>.  Also note that
		/// not all clock devices have alarms.  Check to see if this device has
		/// alarms first by calling the <c>HasClockAlarm()</c> method.
		/// </summary>
		/// <param name="alarmEnable">true to enable the clock alarm</param>
		/// <param name="state">current state of the device returned from <c>ReadDevice()</c></param>	
		/// <exception cref="OneWireException">if this device does not have clock alarms</exception>
		void SetClockAlarmEnable( Boolean alarmEnable, byte[] state );
	}
}
