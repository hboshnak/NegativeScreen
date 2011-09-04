﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;


namespace NegativeScreen
{
	class NegativeOverlay : Form
	{
		private IntPtr hwndMag;
		private const int HOTKEY_ID = 57768;//random id

		/// <summary>
		/// will the loop be a bruteforce loop ? (no thread sleep at all)
		/// </summary>
		private bool DONTSTOPMENAAAO = true;
		private int refreshInterval = 0;

		public NegativeOverlay(int refreshInterval = 0)
			: base()
		{

			if (refreshInterval <= 0)
			{
				DONTSTOPMENAAAO = true;
			}
			else
			{
				DONTSTOPMENAAAO = false;
				this.refreshInterval = refreshInterval;
			}
			this.TopMost = true;
			this.FormBorderStyle = FormBorderStyle.None;
			this.WindowState = FormWindowState.Maximized;

			if (!NativeMethods.RegisterHotKey(this.Handle, HOTKEY_ID, KeyModifiers.MOD_SHIFT, Keys.Escape))
			{
				throw new Exception("RegisterHotKey()");
			}

			if (!NativeMethods.MagInitialize())
			{
				throw new Exception("MagInitialize()");
			}

			IntPtr hInst = NativeMethods.GetModuleHandle(null);
			IntPtr hwndHost = this.Handle;


			//// Create the host window. 
			//var hwndHost = CreateWindowEx(WS_EX_TOPMOST | WS_EX_LAYERED,
			//    WindowClassName, WindowTitle,
			//    WS_CLIPCHILDREN,
			//    0, 0, 0, 0,
			//    NULL, NULL, hInstance, NULL);
			//if (!hwndHost)
			//{
			//    return FALSE;
			//}

			if (NativeMethods.SetWindowLong(hwndHost, NativeMethods.GWL_EXSTYLE, (int)ExtendedWindowStyles.WS_EX_LAYERED | (int)ExtendedWindowStyles.WS_EX_TRANSPARENT) == 0)
			{
				throw new Exception("SetWindowLong()");
			}

			// Make the window opaque.
			if (!NativeMethods.SetLayeredWindowAttributes(hwndHost, 0, 255, LayeredWindowAttributeFlags.LWA_ALPHA))
			{
				throw new Exception("SetLayeredWindowAttributes()");
			}

			//// Create a magnifier control that fills the client area.
			//hwndMag = CreateWindow(WC_MAGNIFIER, TEXT("MagnifierWindow"),
			//    WS_CHILD | MS_SHOWMAGNIFIEDCURSOR | WS_VISIBLE,
			//    0, 0,
			//    LENS_WIDTH,
			//    LENS_HEIGHT,
			//    hwndHost, NULL, hInstance, NULL);

			// Create a magnifier control that fills the client area.
			hwndMag = NativeMethods.CreateWindowEx(0,
				NativeMethods.WC_MAGNIFIER,
				"MagnifierWindow",
				(int)WindowStyles.WS_CHILD |
				/*(int)MagnifierStyle.MS_SHOWMAGNIFIEDCURSOR |*/
				(int)WindowStyles.WS_VISIBLE |
			(int)MagnifierStyle.MS_INVERTCOLORS,
				0, 0, Screen.GetBounds(this).Right, Screen.GetBounds(this).Bottom,
				hwndHost, IntPtr.Zero, hInst, IntPtr.Zero);

			if (hwndMag == IntPtr.Zero)
			{
				throw new Exception("CreateWindowEx()");
			}

			// Set the magnification factor.
			//Transformation matrix = new Transformation(1.0f);

			//var d = NativeMethods.MagSetWindowTransform(hwndMag, ref matrix);

			// Reclaim topmost status, to prevent unmagnified menus from remaining in view. 
			//var f = NativeMethods.SetWindowPos(form.Handle, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
			//    (int)SetWindowPosFlags.SWP_NOACTIVATE | (int)SetWindowPosFlags.SWP_NOMOVE | (int)SetWindowPosFlags.SWP_NOSIZE);


			bool preventFading = true;
			if (NativeMethods.DwmSetWindowAttribute(this.Handle, DWMWINDOWATTRIBUTE.DWMWA_EXCLUDED_FROM_PEEK, ref preventFading, sizeof(int)) != 0)
			{
				throw new Exception("DwmSetWindowAttribute(DWMWA_EXCLUDED_FROM_PEEK)");
			}
			
			DWMFLIP3DWINDOWPOLICY threeDPolicy = DWMFLIP3DWINDOWPOLICY.DWMFLIP3D_EXCLUDEABOVE;
			if (NativeMethods.DwmSetWindowAttribute(this.Handle, DWMWINDOWATTRIBUTE.DWMWA_FLIP3D_POLICY, ref threeDPolicy, sizeof(int)) != 0)
			{
				throw new Exception("DwmSetWindowAttribute(DWMWA_FLIP3D_POLICY)");
			}

			bool disallowPeek = true;
			if (NativeMethods.DwmSetWindowAttribute(this.Handle, DWMWINDOWATTRIBUTE.DWMWA_DISALLOW_PEEK, ref disallowPeek, sizeof(int)) != 0)
			{
				throw new Exception("DwmSetWindowAttribute(DWMWA_DISALLOW_PEEK)");
			}

			

			this.Show();

			// Force redraw.
			while (true)
			{
				// Reclaim topmost status, to prevent unmagnified menus from remaining in view. 
				
				try
				{
					if (!NativeMethods.SetWindowPos(this.Handle, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
				   (int)SetWindowPosFlags.SWP_NOACTIVATE | (int)SetWindowPosFlags.SWP_NOMOVE | (int)SetWindowPosFlags.SWP_NOSIZE))
					{
						throw new Exception("SetWindowPos()");
					}
				}
				catch (ObjectDisposedException)
				{
					//application is exiting
				}
				catch (Exception)
				{
					throw;
				}
				NativeMethods.InvalidateRect(hwndMag, IntPtr.Zero, true);
				Application.DoEvents();
				if (!DONTSTOPMENAAAO)
				{
					System.Threading.Thread.Sleep(refreshInterval);
				}
				//GC.Collect();
			}

		}

		protected override void WndProc(ref Message m)
		{
			// Listen for operating system messages.
			switch (m.Msg)
			{
				case (int)WindowMessage.WM_HOTKEY:
					if ((int)m.WParam == HOTKEY_ID)
					{
						NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_ID);
						Application.Exit();
					}
					break;
			}
			base.WndProc(ref m);
		}

		protected override void Dispose(bool disposing)
		{
			NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_ID);
			NativeMethods.MagUninitialize();
			base.Dispose(disposing);
		}

	}
}