using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TACore {
    public class WoWRealm : ISettingsFileProvider {

        public static List<WoWRealm> RealmsInAccount(WoWAccount account) {

            string realmsFolderPath = account.FullPathToFileContainer();

            List<WoWRealm> realms = new List<WoWRealm>();

            if (Directory.Exists(realmsFolderPath)) {

                DirectoryInfo realmsFolder = new DirectoryInfo(realmsFolderPath);

                foreach (DirectoryInfo realmFolder in realmsFolder.GetDirectories()) {

                    if (!realmFolder.Name.Equals(Constants.kSavedVariablesFolderName)) {

                        WoWRealm newRealm = new WoWRealm(account, realmFolder.Name);

                        if ((newRealm.SettingsFiles.Count + newRealm.Characters.Count) > 0) {
                            realms.Add(newRealm);
                        }
                    }
                }
            }

            return realms;

        }



        public WoWRealm(WoWAccount account, string realmName) {
            Account = account;
            RealmName = realmName;
            SettingsFiles = SettingsFile.SettingsFilesInContainer(this);
            Characters = WoWCharacter.CharactersInRealm(this);
        }


        public string FullPathToFileContainer() {

            return Path.Combine(
                Account.FullPathToFileContainer(),
                RealmName
                );
        }

        public WoWCharacter AddCharacterWithName(string characterName) {
            WoWCharacter character = new WoWCharacter(this, characterName);
            List<WoWCharacter> characters = Characters;
            characters.Add(character);
            Characters = characters;
            return character;
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

            foreach (WoWCharacter character in Characters) {
                character.GenerateSideCarFiles();
            }

        }

        public void RemoveAllSideCarFiles() {

            foreach (SettingsFile file in SettingsFiles) {
                file.RemoveSideCarFile();
            }

            foreach (WoWCharacter character in Characters) {
                character.RemoveAllSideCarFiles();
            }
        }


        #region Properties

        public List<WoWCharacter> Characters {
            get;
            private set;
        }

        public List<SettingsFile> SettingsFiles {
            get;
            private set;
        }

        public WoWAccount Account {
            get;
            private set;
        }

        public string RealmName {
            get;
            private set;
        }

        #endregion
    }
}
