using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace timmer
{


    class Program
    {
        [Verb("extract", HelpText = "Extract graphic.")]
        internal class ExtractOptions
        {
            [Option('i', "infile", Required = false, HelpText = "Filename to extract graphic from.")]
            public string Infilename { get; set; }

            [Option('o', "outfile", Required = false, HelpText = "Filename to save graphic to (only PNGs are supported).")]
            public string Outfilename { get; set; }

            [Option('p', "pixeldata", Required = false, HelpText = "Position of pixel data.")]
            public string PixelPos { get; set; }
            
            [Option('c', "clutdata", Required = false, HelpText = "Position of clut/palette data.")]
            public string PalettePos { get; set; }

            [Option('b', "bpp", Required = false, HelpText = "Bits per pixel.")]
            public uint BPP { get; set; }

            [Option('w', "width", Required = false, HelpText = "Width of image.")]
            public ushort Width { get; set; }

            [Option('h', "height", Required = false, HelpText = "Height of image.")]
            public ushort Height { get; set; }

            [Option('t', "timdata", Required = false, HelpText = "Position of tim data.")]
            public string TimPos { get; set; }

            [Option('s', "stp", Required = false, Default = false, HelpText = "Whether or not to use STP. Defaults false")]
            public bool STP { get; set; }

        }

        [Verb("insert", HelpText = "Insert graphic.")]
        internal class InsertOptions
        {
            [Option('i', "infile", Required = false, HelpText = "Filename to insert graphic into.")]
            public string Infilename { get; set; }

            [Option('o', "outfile", Required = false, HelpText = "Filename of graphic to insert.")]
            public string Outfilename { get; set; }

            [Option('f', "filter", Required = false, HelpText = "Filter to find files to insert graphics into.")]
            public string Filter { get; set; }

            [Option('p', "pixeldata", Required = false, HelpText = "Position of pixel data of original graphic.")]
            public string PixelPos { get; set; }

            [Option('c', "clutdata", Required = false, HelpText = "Position of clut/palette data of original graphic.")]
            public string PalettePos { get; set; }

            [Option('b', "bpp", Required = false, HelpText = "Bits per pixel.")]
            public uint BPP { get; set; }

            [Option('t', "timdata", Required = false, HelpText = "Position of tim data.")]
            public string TimPos { get; set; }

            [Option('m', "mask", Required = false, HelpText = "Mask to use when inserting pixels.")]
            public uint Mask { get; set; }

            [Option('s', "stp", Required = false, Default = false, HelpText = "Whether or not to use STP. Defaults false")]
            public bool STP { get; set; }
        }

        class TIMEntry
        {
            public int index;
            public uint position;
            public TIM aTim;
        }

        static void Main(string[] args)
        {
            var types = LoadVerbs();
            object parsed = Parser.Default.ParseArguments(args, types).WithParsed(Run);
        }

        private static void Run(object obj)
        {
            switch (obj)
            {
                case ExtractOptions e:
                    Extract(e);
                    break;
                case InsertOptions e:
                    Insert(e);
                    break;
            }

            void Extract(ExtractOptions opts)
            {
                string infilename = opts.Infilename;
                if (!string.IsNullOrEmpty(infilename))
                {
                    List<string> list = new List<string>();
                    if (Directory.Exists(infilename))
                    {
                        list.AddRange(Directory.GetFiles(infilename, "*", SearchOption.AllDirectories));
                    }
                    else
                    {
                        list.Add(infilename);
                    }

                    foreach (string item in list)
                    {
                        Console.WriteLine("Scanning " + item + "...");

                        List<uint> timPositions = new List<uint>();
                        BinaryReader br = new BinaryReader(File.OpenRead(item));
                        while (br.BaseStream.Position + 8 < br.BaseStream.Length)
                        {
                            uint magicId = br.ReadUInt32();
                            uint mode = br.ReadUInt32();
                            if (magicId == 0x0010 && (mode & 7) < 4)
                            {
                                timPositions.Add((uint)(br.BaseStream.Position - 8));
                            }
                            br.BaseStream.Seek(-7, SeekOrigin.Current);
                        }

                        for (int i = 0; i < timPositions.Count; i++)
                        {
                            uint timPos = timPositions[i];
                            br.BaseStream.Seek(timPos, SeekOrigin.Begin);
                            try
                            {
                                TIM tim = new TIM(br, opts.STP);
                                string filename = item + "." + i.ToString("D4") + ".png";
                                tim.ExportPNG(filename);
                            }
                            catch
                            {
                            }
                        }

                        br.Close();
                    }
                }
            }

            void Insert(InsertOptions opts)
            {
                string infilename = opts.Infilename;
                string outfilename = opts.Outfilename;
                if (!string.IsNullOrEmpty(infilename))
                {
                    List<string> infilenames = new List<string>();
                    if (Directory.Exists(infilename))
                    {
                        string searchPattern = "*";
                        if (!string.IsNullOrEmpty(opts.Filter))
                        {
                            searchPattern = opts.Filter;
                        }
                        infilenames.AddRange(Directory.GetFiles(infilename, searchPattern, SearchOption.AllDirectories));
                    }
                    else
                    {
                        infilenames.Add(infilename);
                    }
                    List<string> outfilenames = new List<string>();
                    if (Directory.Exists(outfilename))
                    {
                        outfilenames.AddRange(Directory.GetFiles(outfilename, "*.png", SearchOption.AllDirectories));
                    }
                    else
                    {
                        outfilenames.Add(infilename);
                    }
                    List<string> graphicsToInsert = new List<string>();
                    for (int j = 0; j < outfilenames.Count; j++)
                    {
                        try
                        {
                            string text = outfilenames[j];
                            FileInfo fileInfo = new FileInfo(text.Replace(new Regex(".([^.]+).png").Match(text).Value, ""));
                            if (!graphicsToInsert.Contains(fileInfo.Name))
                            {
                                graphicsToInsert.Add(fileInfo.Name);
                            }
                        }
                        catch
                        {
                            Console.WriteLine("Graphics insertion for " + outfilenames[j] + " not supported.");
                        }
                    }
                    foreach (string file in infilenames)
                    {
                        FileInfo fi = new FileInfo(file);
                        if (graphicsToInsert.Contains(fi.Name))
                        {

                            Console.WriteLine("Updating " + file + "...");

                            List<uint> timPositions = new List<uint>();
                            BinaryReader br = new BinaryReader(File.OpenRead(file));
                            while (br.BaseStream.Position + 8 < br.BaseStream.Length)
                            {
                                uint magicId = br.ReadUInt32();
                                uint mode = br.ReadUInt32();
                                if (magicId == 0x0010 && (mode & 7) < 4)
                                {
                                    timPositions.Add((uint)(br.BaseStream.Position - 8));
                                }
                                br.BaseStream.Seek(-7, SeekOrigin.Current);
                            }

                            List<TIMEntry> tims = new List<TIMEntry>();
                            for (int k = 0; k < timPositions.Count; k++)
                            {
                                uint timPos = timPositions[k];

                                string graphicFilename = new FileInfo(file).Name + "." + k.ToString("D4");
                                string graphicFilenameToInsert = outfilenames.Where((string x) => x.Contains(graphicFilename)).FirstOrDefault();
                                if (!string.IsNullOrEmpty(graphicFilenameToInsert))
                                {
                                    Console.WriteLine("Inserting image " + graphicFilenameToInsert + "...");
                                    br.BaseStream.Seek(timPos, SeekOrigin.Begin);
                                    try
                                    {
                                        TIM tim = new TIM(br, opts.STP);
                                        tim.ImportImage(graphicFilenameToInsert);
                                        tims.Add(new TIMEntry
                                        {
                                            index = k,
                                            position = timPos,
                                            aTim = tim,
                                        });
                                    }
                                    catch
                                    {
                                    }
                                }
                            }
                            br.Close();

                            BinaryWriter bw = new BinaryWriter(File.OpenWrite(file));
                            for (int l = 0; l < tims.Count; l++)
                            {
                                TIMEntry timEntry = tims[l];
                                bw.BaseStream.Seek(timEntry.aTim.GetPixelPos(), SeekOrigin.Begin);
                                bw.Write(timEntry.aTim.GetPixelData());
                            }
                            bw.Close();
                        }
                    }
                }
            }
        }

        //load all Verb types using Reflection
        static Type[] LoadVerbs()
        {
            return Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetCustomAttribute<VerbAttribute>() != null).ToArray();
        }
    }

}
