using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TACore {
    class Constants {

        // Automatic 

        public const double kAutomaticSyncInterval = 60 * 30; // 30 mins
        public const double kAutomaticSyncEventCooldown = 2.0; // Time between something happening and a sync starting
        public const double kWoWProcessPollInterval = 5.0;
        public const double kSyncAnimationFrameDuration = 0.5;

        //Registry

        public const string kTeleportAddonsRegistryKey = "TeleportAddons";
        public const string kTeleportAddonsRegistryValue = "\"{0}\" /HideWindowAtStartup";

        // Prefs

        public const string kSyncAutomaticallyUserDefaultsKey = "SyncAutomatically";
        public const string kHasShownAutoSyncMinimiseWarning = "HasShownAutoSyncMinimiseWarning";

        // Backups

        public const string kBackupsFolderName = "Backups";
        public const string kBackupExpiryTimeUserDefaultsKey = "BackupExpiryTime";

        public const string kBackupPlistDescriptionKey = "backupDescription";
        public const string kBackupPlistBackupPathKey = "fullPathToBackupFile";
        public const string kBackupPlistDateCreatedKey = "dateCreated";

        // Folder Names

        public const string kWoWWTFFolderName = "WTF";
        public const string kWoWWTFAccountSubFolderName = "Account";
        public const string kWoWInterfaceFolderName = "Interface";
        public const string kWoWInterfaceAddonsSubFolderName = "AddOns"; // Careful on case-sensitive file systems!

        // Finding Settings

        public const string kSavedVariablesFolderName = "SavedVariables";
        public const string kSettingsFileNameExclusionFragmentListFileName = "ExcludedFileNameFragments.plist";
        public const string kSettingsFileNameExclusionFragmentListKey = "ExcludedSettingsFileNameFragments";

        // Finding Addons

        public const string kAddonsFileNameExclusionFragmentListFileName = "ExcludedFileNameFragments.plist";
        public const string kAddonsFileNameExclusionFragmentListKey = "ExcludedAddonFileNameFragments";
        public const string kWoWAddonTOCExtension = "toc";
        public const string kWoWTOCVersionPrefix = "## Version";
        public const string kWoWTOCTitlePrefix = "## Title";
        public const string kWoWTOCCurseVersionPrefix = "## X-Curse-Packaged-Version";
        public const string kAddonMissingVersionSubstitutedVersion = "0";

        // General 

        public const string kWorldOfWarcraftApplicationIdentifier = "com.blizzard.worldofwarcraft";
        public const string kTeleportAddonsErrorDomain = "org.danielkennett.TeleportAddons";
        public const string kSyncSourceUserDefaultsKey = "SyncSource";
        public const string kSyncSourceDataUserDefaultsKey = "SyncSourceData";
        public const string kTargetInstallPathUserDefaultsKey = "TargetInstallPath";
        public const string kApplicationSupportFolderName = "TeleportAddons";
        public const string kCachedSyncSourceUserDefaultsKey = "LastBackupId";
        public const string kSyncStoreFileExtension = "teleportAddonsStore";
        public const string kProductWebsiteURL = "http://www.kennettnet.co.uk/products/teleportaddons/";
        public const string kExceptionErrorCodeKey = "kExceptionErrorCodeKey";
        public const string kHideWindowAtStartupArgument = "/HideWindowAtStartup";
        public const string kSkipInitialAutosyncArgument = "/SkipInitialAutosync";
		public const string kTeleportAddonsIdentifier = "org.danielkennett.TeleportAddons";

        // Sidecars

        public const string kSideCarFileSuffix = ".teleportAddonsSideCar";
        public const string kSideCarChecksumKey = "checksum";
        public const string kSideCarDateModifiedUTCKey = "dateModifiedUTC";
        public const string kSideCarDateCreatedUTCKey = "dateCreatedUTC";
        public const string kSideCarSyncSourceSyncIdKey = "id";

        // Sources

        public const string kSyncLockFileName = "TeleportAddonsSyncLock";
        public const string kSyncLockDateLockedKey = "DateLocked";
        public const string kSyncLockMachineIdentifierKey = "MachineIdentifier";

        // File

        public const string kFileSyncStoreStoreFileName = "syncstore.zip";
        public const string kFileSystemSyncSourceLocationKey = "filePath";

        //Dropbox

        public const string kDropBoxSyncStoreStoreDirectoryName = "/TeleportAddons";
        public const string kDropBoxSyncStoreStoreFileName = "TeleportAddons Sync Store.teleportAddonsStore";
        public const string kDropBoxWebsiteURL = "http://dropbox.com/";
        public const string kDropboxSyncSourceUserNameKey = "name";
        public const string kDropboxSyncSourceTokenKey = "token";
        public const string kDropboxSyncSourceSecretKey = "secret";

        // Logs 

        public const uint kLogCount = 10;
        public const string kLogFileExtension = "teleportAddonsLog";
        public const string kLogsFolderName = "Logs";

        public const string kLogPlistStartDateKey = "startDate";
        public const string kLogPlistEndDateKey = "endDate";
        public const string kLogPlistWasTestRunKey = "wasTestRun";
        public const string kLogPlistResultKey = "result";
        public const string kLogPlistStepsKey = "steps";

        public const string kLogStepPlistDescriptionKey = "description";
        public const string kLogStepPlistStatusKey = "status";
        public const string kLogStepPlistChildrenKey = "children";

    }
}
