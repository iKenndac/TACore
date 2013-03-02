using System;

namespace TACore {

	public abstract class Platform {

		static Platform thisPlatform;

		public static Platform CurrentPlatform {
			get {
				if (thisPlatform == null) {
					if (Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix)
						thisPlatform = new MacPlatform(); 
					else
						thisPlatform = new WindowsPlatform();
				}

				return thisPlatform;
			}
		}

		public abstract bool WoWIsOpen { get; }
		public abstract string WoWInstallationDirectory { get; }
		public abstract string ApplicationDataDirectory { get; }

	}
}

