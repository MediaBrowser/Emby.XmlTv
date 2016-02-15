﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Emby.XmlTv.Classes;
using Emby.XmlTv.Console.Classes;
using Emby.XmlTv.Entities;

namespace Emby.XmlTv.Console
{
    public class Program
    {
        static void Main(string[] args)
        {
            var filename = @"C:\Temp\NZ-TVGuide.xml";

            if (args.Length == 1 && File.Exists(args[0]))
            {
                filename = args[0];
            }

            var timer = Stopwatch.StartNew();
            System.Console.WriteLine("Running XMLTv Parsing");

            var resultsFile = String.Format("C:\\Temp\\{0}_Results_{1:HHmmss}.txt", 
                Path.GetFileNameWithoutExtension(filename),
                DateTime.UtcNow);

            ReadSourceXmlTvFile(filename, resultsFile).Wait();

            System.Console.WriteLine("Completed in {0:g} - press any key to open the file...", timer.Elapsed);
            System.Console.ReadKey();

            Process.Start(resultsFile);
        }

        public static async Task ReadSourceXmlTvFile(string filename, string resultsFile)
        {
            System.Console.WriteLine("Writing to file: {0}", resultsFile);

            using (var resultsFileStream = new StreamWriter(resultsFile) { AutoFlush = true })
            {
                var reader = new XmlTvReader(filename);
                await ReadOutChannels(reader, resultsFileStream);

                resultsFileStream.Close();
            }
        }

        public static async Task ReadOutChannels(XmlTvReader reader, StreamWriter resultsFileStream)
        {
            var channels = reader.GetChannels().Distinct().ToList();

            resultsFileStream.Write(EntityExtensions.GetHeader("Channels"));

            foreach (var channel in channels)
            {
                resultsFileStream.Write("{0}\r\n", channel);
            }

            resultsFileStream.Write("\r\n");
            foreach (var channel in channels)
            {
                resultsFileStream.Write(EntityExtensions.GetHeader("Programs for " + channel));
                await ReadOutChannelProgrammes(reader, channel, resultsFileStream);
            }
        }

        private static async Task ReadOutChannelProgrammes(XmlTvReader reader, XmlTvChannel channel, StreamWriter resultsFileStream)
        {
            //var startDate = new DateTime(2015, 11, 28);
            //var endDate = new DateTime(2015, 11, 29);
            var startDate = DateTime.MinValue;
            var endDate = DateTime.MaxValue;

            foreach (var programme in reader.GetProgrammes(channel.Id, startDate, endDate, new CancellationToken()).Distinct())
            {
                await resultsFileStream.WriteLineAsync(programme.GetProgrammeDetail(channel));
            }
        }
    }
}