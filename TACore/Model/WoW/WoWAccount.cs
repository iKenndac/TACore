using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TACore {
    public class WoWAccount : ISettingsFileProvider {

        public static List<WoWAccount> AccountsInInstall(WoWInstall install) {

            string accountsFolderPath = Path.Combine(
                install.DirectoryPath,
                Constants.kWoWWTFFolderName,
                Constants.kWoWWTFAccountSubFolderName
                );

            List<WoWAccount> accounts = new List<WoWAccount>();

            if (Directory.Exists(accountsFolderPath)) {

                DirectoryInfo accountsFolder = new DirectoryInfo(accountsFolderPath);

                foreach (DirectoryInfo accountFolder in accountsFolder.GetDirectories()) {

                    if (!accountFolder.Name.Equals(Constants.kSavedVariablesFolderName)) {

                        WoWAccount newAccount = new WoWAccount(install, accountFolder.Name);

                        if ((newAccount.SettingsFiles.Count + newAccount.Realms.Count)  > 0 ) {
                            accounts.Add(newAccount);
                        }
                    }
                }
            }

            return accounts;

        }



        public WoWAccount(WoWInstall anInstall, string anAccountName) {
            Install = anInstall;
            AccountName = anAccountName;
            SettingsFiles = SettingsFile.SettingsFilesInContainer(this);
            Realms = WoWRealm.RealmsInAccount(this);
        }

        public WoWRealm AddRealmWithName(string realmName) {
            WoWRealm realm = new WoWRealm(this, realmName);
            List<WoWRealm> realms = Realms;
            realms.Add(realm);
            Realms = realms;
            return realm;
        }

        public string FullPathToFileContainer() {

            return Path.Combine(
                Install.DirectoryPath,
                Constants.kWoWWTFFolderName,
                Constants.kWoWWTFAccountSubFolderName,
                AccountName
                );
        }

        public SettingsFile AddSettingsFileWithRelativePathToContainer(string relativePath) {

            SettingsFile file = new SettingsFile(relativePath, this);
            List<SettingsFile> files = SettingsFiles;
            files.Add(file);
            SettingsFiles = files;
            return file;
        }

        public void GenerateSideCarFiles() {

            foreach (SettingsFile file in SettingsFiles) {
                file.GenerateSideCarFile();
            }

            foreach (WoWRealm realm in Realms) {
                realm.GenerateSideCarFiles();
            }

        }

        public void RemoveAllSideCarFiles() {

            foreach (SettingsFile file in SettingsFiles) {
                file.RemoveSideCarFile();
            }

            foreach (WoWRealm realm in Realms) {
                realm.RemoveAllSideCarFiles();
            }
        }


        #region Properties

        public List<WoWRealm> Realms {
            get;
            private set;
        }

        public List<SettingsFile> SettingsFiles {
            get;
            private set;
        }

        public WoWInstall Install {
            get;
            private set;
        }

        public string AccountName {
            get;
            private set;
        }

        #endregion
    }
}
