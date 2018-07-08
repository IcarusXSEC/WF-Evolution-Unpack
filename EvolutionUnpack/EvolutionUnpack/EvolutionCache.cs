using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using GMWare.IO;

namespace EvolutionUnpack
{
	public class EvolutionCache : IDisposable
    {
        static readonly uint MagicNumber = 0x1867C64E;

        Stream packToc;
        Stream packContent;
        BinaryReader tocReader;
        BinaryWriter tocWriter;
        int archiveVersion;

        List<FileEntry> files = new List<FileEntry>();
        List<FileEntry> directories = new List<FileEntry>();
        Dictionary<int, string> dirPathCache = new Dictionary<int, string>();

        public IEnumerable<string> FileList
        {
            get
            {
                foreach (FileEntry file in files)
                {
                    yield return constructFilePath(file);
                }
            }
        }

        public EvolutionCache(Stream tocStream, Stream contentStream)
        {
            if (tocStream == null) throw new ArgumentNullException("tocStream");
            if (contentStream == null) throw new ArgumentNullException("contentStream");
            packToc = tocStream;
            packContent = contentStream;
            tocReader = new BinaryReader(tocStream);
        }

        public void ReadDirectory()
        {
            // Reset pack
            packToc.Seek(0, SeekOrigin.Begin);
            files.Clear();
            directories.Clear();
            dirPathCache.Clear();

            // Verify archive type
            if (tocReader.ReadUInt32() != MagicNumber) throw new InvalidDataException("Archive is not Evolution Engine cache.");
            archiveVersion = tocReader.ReadInt32();
            if (!canReadVersion(archiveVersion)) throw new InvalidDataException("Cannot read this archive version.");

            int nextDirIndex = 0;

            // Add dummy root directory
            directories.Add(new FileEntry() { Offset = -1, Date = DateTime.MinValue, CompressedLength = -1, Length = -1, ScopeIndex = -1, ParentDirectoryIndex = -1, FileName = string.Empty });
            dirPathCache[nextDirIndex] = string.Empty;
            ++nextDirIndex;

            // Load file entries
            while (packToc.Position < packToc.Length)
            {
                long offset = tocReader.ReadInt64();
                long fileTime = tocReader.ReadInt64();
                DateTime modTime = fileTime == -1 ? DateTime.MinValue : DateTime.FromFileTime(fileTime);
                int compLen = tocReader.ReadInt32();
                int len = tocReader.ReadInt32();
                int reserved = tocReader.ReadInt32();
                int parentId = tocReader.ReadInt32();
                string fileName = new string(tocReader.ReadChars(64)).TrimEnd('\0'); // Assume ASCII only file names
                FileEntry entry = new FileEntry() { Offset = offset, Date = modTime, CompressedLength = compLen, Length = len, ScopeIndex = reserved, ParentDirectoryIndex = parentId, FileName = fileName };
                //Console.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}", offset, modTime, compLen, len, reserved, parentId, fileName);
                if (reserved != 0 && !entry.IsDirectory) Console.WriteLine("File {0} has non-zero scope index 0x{1:x8}.", fileName, reserved);
                if (entry.IsDirectory)
                {
                    directories.Add(entry);
                    dirPathCache[nextDirIndex] = Path.Combine(dirPathCache[parentId], fileName);
                    ++nextDirIndex;
                }
                else
                {
                    files.Add(entry);
                }
            }
        }

        bool canReadVersion(int version)
        {
        	return (version == 20 || version == 16);
        }
        
        string constructFilePath(FileEntry entry)
        {
            return Path.Combine(dirPathCache[entry.ParentDirectoryIndex], entry.FileName);
        }

        public void ExtractFile(string basePath, string fileName)
        {
            string fileDirPath = Path.GetDirectoryName(fileName);
            string fileNameOnly = Path.GetFileName(fileName);
            int parentPath = (from pDir in dirPathCache
                              where pDir.Value == fileName
                              select pDir.Key).FirstOrDefault();
            FileEntry fEntry = (from entry in files
                                where entry.FileName == fileNameOnly && entry.ParentDirectoryIndex == parentPath
                                select entry).FirstOrDefault();
            if (fEntry == null) throw new FileNotFoundException(string.Format("Cannot find file {0} in cache.", fileName));
        }

        void ExtractFile(string basePath, FileEntry entry)
        {
            string filePath = Path.Combine(dirPathCache[entry.ParentDirectoryIndex], entry.FileName);
            Console.WriteLine("Extracting {0}", filePath);
            Directory.CreateDirectory(Path.Combine(basePath, dirPathCache[entry.ParentDirectoryIndex]));
            using (FileStream fstream = File.Create(Path.Combine(basePath, filePath)))
            {
                packContent.Seek(entry.Offset, SeekOrigin.Begin);
                if (entry.Length == entry.CompressedLength)
                {
                    StreamUtils.StreamCopyWithLength(packContent, fstream, entry.CompressedLength);
                }
                else
                {
                    try
                    {
                        int i = 0;
                        while (i < entry.Length)
                        {
                            // Get block lengths
                            byte blockLenHi = (byte)packContent.ReadByte();
                            byte blockLenLo = (byte)packContent.ReadByte();
                            byte decompLenHi = (byte)packContent.ReadByte();
                            byte decompLenLo = (byte)packContent.ReadByte();
                            int blockLen = (blockLenHi<<8) | blockLenLo;
                            int decompLen = (decompLenHi << 8) | decompLenLo;
                            byte[] compressed = new byte[blockLen];
                            byte[] decompressed = new byte[decompLen];
                            packContent.Read(compressed, 0, blockLen);
                            if (blockLen != decompLen)
                            {
                                LZCoder.Decompress(compressed, decompressed);
                                fstream.Write(decompressed, 0, decompLen);
                            }
                            else
                            {
                                fstream.Write(compressed, 0, blockLen); // No decompression for this block.
                            }
                            i += decompLen;
                        }
                        if (i != entry.Length) throw new InvalidDataException("Decompressed length does not match length in file entry.");
                    }
                    catch
                    {
                        Console.WriteLine("Failed to decompress {0}", filePath);
                        throw;
                    }
                }
                fstream.Flush();
            }
            File.SetLastWriteTime(Path.Combine(basePath, filePath), entry.Date);
        }

        public void ExtractAll(string basePath)
        {
            foreach (FileEntry entry in files)
            {
                try
                {
                    ExtractFile(basePath, entry);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error extracting file: {0}", e.Message);
                }
            }
        }
		
		public void Dispose()
		{
			packToc.Close();
			packContent.Close();
		}
    }
}
