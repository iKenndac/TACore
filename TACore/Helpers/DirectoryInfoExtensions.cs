using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;

public static class DirectoryInfoExtensions {

    public static void CopyTo(this DirectoryInfo o, string path) {

        // Check if the target directory exists, if not, create it.
        if (Directory.Exists(path) == false) {
            Directory.CreateDirectory(path);
        }

        // Copy each file into its new directory.
        foreach (FileInfo fi in o.GetFiles()) {
           fi.CopyTo(Path.Combine(path, fi.Name), true);
            
        }

        // Copy each subdirectory using recursion.
        foreach (DirectoryInfo sourceDirectory in o.GetDirectories()) {
            DirectoryInfo targetDirectory = Directory.CreateDirectory(Path.Combine(path, sourceDirectory.Name));
            sourceDirectory.CopyTo(targetDirectory.FullName);
        }
    }

    public static void TryToMoveTo(this DirectoryInfo o, string targetPath) {

        int attemptsRemaining = 5;

        while (true) {
            try {
                o.MoveTo(targetPath);
                break;

            } catch (Exception) {

                if (attemptsRemaining == 0) {
                    throw;
                } else {
                    attemptsRemaining--;
                    System.Threading.Thread.Sleep(10);
                }
            }
        }
    }

    public static void TryToDelete(this DirectoryInfo o) {

        int attemptsRemaining = 5;

        while (true) {
            try {
                o.Delete(true);
                break;

            } catch (Exception) {

                if (attemptsRemaining == 0) {
                    throw;
                } else {
                    attemptsRemaining--;
                    System.Threading.Thread.Sleep(10);
                }
            }
        }
    }

    public static string MD5Hash(this DirectoryInfo o) {

        string[] hashes = o.HashesForFiles();

        MemoryStream stream = new MemoryStream();

        foreach (string hash in hashes) {
            byte[] bytes = Encoding.UTF8.GetBytes(hash);
            stream.Write(bytes, 0, bytes.Length);
        }

        MD5 md5 = MD5.Create();
        byte[] finalHash = md5.ComputeHash(stream.ToArray());

        // Build the final string by converting each byte
        // into hex and appending it to a StringBuilder
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < finalHash.Length; i++) {
            sb.Append(finalHash[i].ToString("x2"));
        }

        return sb.ToString();
    }

    public static string[] HashesForFiles(this DirectoryInfo o) {

        // Check if the target directory exists, if not, return null
        if (Directory.Exists(o.FullName) == false) {
            return null;
        }

        List<string> hashes = new List<string>();
        
        // Copy each file into it’s new directory.
        foreach (FileInfo fi in o.GetFiles()) {

            if (!fi.Name.StartsWith(".")) {

                byte[] bytes = File.ReadAllBytes(Path.Combine(o.FullName, fi.Name));

                if (bytes != null) {
                    MD5 md5 = MD5.Create();
                    byte[] hash = md5.ComputeHash(bytes);

                    // Build the final string by converting each byte
                    // into hex and appending it to a StringBuilder
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hash.Length; i++) {
                        sb.Append(hash[i].ToString("x2"));
                    }

                    hashes.Add(sb.ToString());
                }
            }
        }

        // Copy each subdirectory using recursion.
        foreach (DirectoryInfo diSourceSubDir in o.GetDirectories()) {

            string[] directoryHashes = diSourceSubDir.HashesForFiles();

            if (directoryHashes != null) {
                hashes.AddRange(directoryHashes);
            }

        }

        hashes.Sort();

        return hashes.ToArray();
    }


    public static void NewestFileDatesUTC(this DirectoryInfo o, out DateTime newestModificationDate, out DateTime newestCreationDate) {

        DateTime newestModificationTime = DateTime.MinValue;
        DateTime newestCreatedTime = DateTime.MinValue;

        if (o.Exists) {

            foreach (FileInfo fi in o.GetFiles()) {

                try {

                    DateTime fileCreated = fi.CreationTimeUtc;
                    if (fileCreated > newestCreatedTime) {
                        newestCreatedTime = fileCreated;
                    }

                    DateTime fileModified = fi.LastWriteTimeUtc;
                    if (fileModified > newestModificationTime) {
                        newestModificationTime = fileModified;
                    }

                } catch (Exception) { }
            }

            foreach (DirectoryInfo di in o.GetDirectories()) {

                DateTime directoryNewestCreated;
                DateTime directoryNewestModified;

                di.NewestFileDatesUTC(out directoryNewestModified, out directoryNewestCreated);

                if (directoryNewestCreated > newestCreatedTime) {
                    newestCreatedTime = directoryNewestCreated;
                }

                if (directoryNewestModified > newestModificationTime) {
                    newestModificationTime = directoryNewestModified;
                }
            }
        }

        newestCreationDate = newestModificationTime;
        newestModificationDate = newestModificationTime;
        
    }



}
