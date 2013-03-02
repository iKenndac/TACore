using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using KNFoundation;

namespace TACore {
    class BackupController {

        private static BackupController sharedInstance;

        public static BackupController SharedInstance() {
            if (sharedInstance == null) {
                sharedInstance = new BackupController();
            }
            return sharedInstance;
        }

        private BackupController() {

            List<Backup> backups = new List<Backup>();

            string backupsPath = BackupsPath();
            DirectoryInfo backupsInfo = new DirectoryInfo(backupsPath);
            if (backupsInfo.Exists) {
                foreach (FileInfo file in backupsInfo.GetFiles()) {

                    if (file.Name.Contains(Constants.kSideCarFileSuffix)) {

                        try {

                            string backupPath = Path.Combine(
                                file.DirectoryName, 
                                Path.GetFileNameWithoutExtension(file.FullName)
                                );

                            if (File.Exists(backupPath)) {

                                Dictionary<string, object> plist =
                                    KNPropertyListSerialization.PropertyListWithData(File.ReadAllBytes(file.FullName));
                                Backup backup = new Backup(plist, backupPath);
                                backups.Add(backup);
                            }
                        } catch (Exception) {
                        }
                    }
                }
            }

            backups.Sort(delegate(Backup backup1, Backup backup2) {
                return backup1.DateCreated.CompareTo(backup2.DateCreated) * -1;
            });

            Backups = backups;
			BackupExpiryTime = TimeSpan.FromDays(30);
        }

        ~BackupController() {
            CleanupExpiredBackups();
        }

        public Backup AddBackupByBackingUpInstallWithDescription(WoWInstall install, string backupDescription) {

            // MIGRATION: This used to will/didChangeValueForKey on Backups

            DateTime now = DateTime.Now;
            string backupFilePath = PathForNewBackupWithDescriptionAtDate(backupDescription, now);

            if (File.Exists(backupFilePath)) {
                throw new Exception(KNBundleGlobalHelpers.KNLocalizedString("backup file already exists error title", ""));
            }

            Helpers.ArchiveInstallToZipFileAtDirectoryForBackup(install, backupFilePath);
            Backup backup = new Backup(backupFilePath, backupDescription, now);

            byte[] plistRep = KNPropertyListSerialization.DataWithPropertyList(backup.PlistRepresentation());
            File.WriteAllBytes(String.Concat(backupFilePath, Constants.kSideCarFileSuffix), plistRep);

            Backups.Add(backup);
            Backups.Sort(delegate(Backup backup1, Backup backup2) {
                return backup1.DateCreated.CompareTo(backup2.DateCreated) * -1;
            });

			// -- DidChange was here

            return backup;
        }

        public void RemoveBackup(Backup backup) {

			// MIGRATION: This used to will/didChangeValueForKey on Backups

            if (Backups.Contains(backup)) {

                if (File.Exists(backup.FullPathToBackupFile)) {
                    try {
                        File.Delete(backup.FullPathToBackupFile);
                    } catch (Exception) {
                    }
                }

                string sidecarFile = String.Concat(backup.FullPathToBackupFile, Constants.kSideCarFileSuffix);

                if (File.Exists(sidecarFile)) {
                    try {
                        File.Delete(sidecarFile);
                    } catch (Exception) {
                    }
                }

                Backups.Remove(backup);

				// -- didChange was here
            }
        }

        public void CleanupExpiredBackups() {

            if (BackupExpiryTime.TotalSeconds > 0) {

                if (Backups.Count > 0) {

                    List<Backup> backups = new List<Backup>(Backups);
                    backups.Sort(delegate(Backup backup1, Backup backup2) {
                        return backup1.DateCreated.CompareTo(backup2.DateCreated);
                    });

                    backups.RemoveAt(backups.Count - 1); // Never remove the latest backup

                    foreach (Backup backup in backups) {

                        TimeSpan difference = DateTime.Now.Subtract(backup.DateCreated);
                        if (difference >= BackupExpiryTime) {
                            RemoveBackup(backup);
                        }
                    }
                }
            }
        }

        private string BackupsPath() {

            string appDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string backupsFolderPath = Path.Combine(appDataFolderPath, Constants.kApplicationSupportFolderName, Constants.kBackupsFolderName);

            if (!Directory.Exists(backupsFolderPath)) {
                try {
                    Directory.CreateDirectory(backupsFolderPath);
                } catch (Exception) {
                    backupsFolderPath = Path.GetTempPath();
                }
            }

            return backupsFolderPath;
        }

        private string PathForNewBackupWithDescriptionAtDate(string desc, DateTime aDate) {

            string fileName = String.Format(KNBundleGlobalHelpers.KNLocalizedString("backup file name formatter", ""),
                desc,
                aDate,
                aDate);

            fileName = fileName.Replace("/", "-");
            fileName = fileName.Replace(":", "-");

            string backupFilePath = Helpers.PathByAppendingUniqueFileNameFromFileNameInPath(fileName, BackupsPath());
            return backupFilePath;
        }

        #region Properties

        public List<Backup> Backups {
            get;
            private set;
        }

		public TimeSpan BackupExpiryTime { get; set; }

        #endregion
    }
}
