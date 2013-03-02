using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace TACore {

	public class WindowsPlatform : Platform {

		public override string WoWInstallationDirectory {
			get {

				try {
					RegistryKey InstallPath = Registry.LocalMachine;
					
					if (Environment.Is64BitOperatingSystem && Environment.Is64BitProcess) {
						InstallPath = InstallPath.OpenSubKey(@"SOFTWARE\Wow6432Node\Blizzard Entertainment\World of Warcraft");
					} else {
						InstallPath = InstallPath.OpenSubKey(@"SOFTWARE\Blizzard Entertainment\World of Warcraft");
					}
					
					return (string)InstallPath.GetValue("InstallPath");
					
				} catch (Exception) {
				}
				
				return null;

			}
		}

		public override bool WoWIsOpen {
			get {
				try {
					Process[] processes = Process.GetProcessesByName("wow");
					if (processes != null && processes.Length > 0) {
						return true;
					}
				} catch (Exception) { }
				return false;
			}
		}

		public override string ApplicationDataDirectory {
			get {
				return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			}
		}

	}
}

