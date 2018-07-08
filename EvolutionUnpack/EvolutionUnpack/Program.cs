using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EvolutionUnpack
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Evolution Engine Cache Extractor 1.0");
			Console.WriteLine("(C) 2013 GMMan");
			Console.WriteLine();

			string filePath = string.Empty;
			string outDir = string.Empty;

			if (args.Length == 0) usage();
			foreach (string arg in args)
			{
				string[] argSplit = arg.Split(new char[] { '=' }, 2);
				if (argSplit.Length != 2)
				{
					if (!string.IsNullOrEmpty(filePath))
					{
						Console.WriteLine("Archive path is specified more than once.");
						Console.WriteLine();
						usage();
					}
					else
						filePath = arg;
				}
				else
				{
					switch (argSplit[0])
					{
						case "/d":
							if (!string.IsNullOrEmpty(outDir))
							{
								Console.WriteLine("Output directory path is specified more than once.");
								Console.WriteLine();
								usage();
							}
							else
								outDir = argSplit[1];
							break;
						default:
							Console.WriteLine("Unknown option {0}.", argSplit[0]);
							Console.WriteLine();
							usage();
							break;
					}
				}
			}

			if (string.IsNullOrEmpty(filePath))
			{
				Console.WriteLine("Archive path is not specified.");
				Console.WriteLine();
				usage();
			}

			if (string.IsNullOrEmpty(outDir)) outDir += filePath + "_extracted";

			EvolutionCache arch = null;
			try
			{
				if (!filePath.ToLower().EndsWith(".toc") && !filePath.ToLower().EndsWith(".cache")) filePath += ".cache"; // In case someone omits the file extension
				string tocPath = Path.ChangeExtension(filePath, ".toc");
				string cachePath = Path.ChangeExtension(filePath, ".cache");
				arch = new EvolutionCache(File.Open(tocPath, FileMode.OpenOrCreate, FileAccess.ReadWrite), File.Open(cachePath, FileMode.OpenOrCreate, FileAccess.ReadWrite));
				arch.ReadDirectory();
			}
			catch (Exception e)
			{
				Console.WriteLine("Error opening archive. Please make sure that you can read the archive, and that it is actually an Evolution Engine cache. ({0})", e.Message);
				// Debug workaround for SharpDevelop so I can see exceptions without getting rid of the catch block
				if (System.Diagnostics.Debugger.IsAttached)
				{
					Console.WriteLine(e.ToString());
					Console.ReadKey();
				}
				Environment.Exit(2);
			}

			try
			{
				arch.ExtractAll(outDir);
			}
			catch (Exception e)
			{
				Console.WriteLine("Error while extracting files. ({0})", e.Message);
				if (System.Diagnostics.Debugger.IsAttached)
				{
					Console.WriteLine(e.ToString());
					Console.ReadKey();
				}
				Environment.Exit(2);
			}

			Console.WriteLine("Extraction complete.");
		}

		static void usage()
		{
			Console.WriteLine("{0} [/d=outputDir] cachePath", System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetExecutingAssembly().Location));
			Console.WriteLine("\tcachePath\tPath to cache file.");
			Console.WriteLine("\t/d=outputDir\tOutput directory path. By default it's the cache name plus \"_extracted\".");
			Environment.Exit(1);
		}
	}
}
