using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TeleportAddons {

    public class WoWCharacter : ISettingsFileProvider {

        public static List<WoWCharacter> CharactersInRealm(WoWRealm realm) {

            string charactersFolderPath = realm.FullPathToFileContainer();
            List<WoWCharacter> characters = new List<WoWCharacter>();

            if (Directory.Exists(charactersFolderPath)) {

                DirectoryInfo charactersFolder = new DirectoryInfo(charactersFolderPath);

                foreach (DirectoryInfo characterFolder in charactersFolder.GetDirectories()) {

                    if (!characterFolder.Name.Equals(Constants.kSavedVariablesFolderName)) {

                        WoWCharacter newCharacter = new WoWCharacter(realm, characterFolder.Name);

                        if (newCharacter.SettingsFiles.Count > 0) {
                            characters.Add(newCharacter);
                        }
                    }
                }
            }

            return characters;

        }

        public WoWCharacter(WoWRealm parentRealm, string characterName) {
            Realm = parentRealm;
            CharacterName = characterName;
            SettingsFiles = SettingsFile.SettingsFilesInContainer(this);
        }


        public string FullPathToFileContainer() {

            return Path.Combine(
                Realm.FullPathToFileContainer(),
                CharacterName
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

        }

        public void RemoveAllSideCarFiles() {

            foreach (SettingsFile file in SettingsFiles) {
                file.RemoveSideCarFile();
            }
        }


        #region Properties

        public List<SettingsFile> SettingsFiles {
            get;
            private set;
        }

        public WoWRealm Realm {
            get;
            private set;
        }

        public string CharacterName {
            get;
            private set;
        }

        #endregion

    }
}
