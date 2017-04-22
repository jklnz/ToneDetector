﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using log4net;
using NAudio.Wave;

namespace Mp3Reader
{
    public class DispatchMessageRecorder : IDisposable
    {
        private const double SilenceThreshold = 0.01;
        private int _lengthOfSilenceInSamples;

        private readonly WaveFileWriter _writer;
        private bool _disposed;
        private readonly DateTime _recordingStartTimeUtc;
        
        public bool IsFinishedRecording { get; private set; }
        public string FileName => _writer.Filename;
        private FileTransferTrigger _fileTransferTrigger;

        private static readonly ILog Log =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public DispatchMessageRecorder(WaveFormat format)
        {
            IsFinishedRecording = false;
            _lengthOfSilenceInSamples = 0;
            _recordingStartTimeUtc = DateTime.UtcNow;

            var apiBaseUri = ConfigurationManager.AppSettings["ApiBaseUri"];
            var outputPath = ConfigurationManager.AppSettings["OutputDirectory"];
            var outputDirectory = new DirectoryInfo(outputPath);
            if (!outputDirectory.Exists) outputDirectory.Create();

            _fileTransferTrigger = new FileTransferTrigger(apiBaseUri, outputPath);
            _writer = new WaveFileWriter(GetFileNameWithRelativePath(outputPath), format);
        }
        
        public void Record(byte[] rawData, int byteCount, float[] samples, int sampleCount)
        {
            if (!Done(samples))
            {
                _writer.Write(rawData, 0, byteCount);

            }
            else
            {
                IsFinishedRecording = true;
                _writer.Close();
                _fileTransferTrigger.SweepOutputDirectory();
                Log.Info($"Finished recording {_recordingStartTimeUtc}");
            }
        }

        private static string GetFileNameWithRelativePath(string path)
        {
            return $"{path}\\dispatch_{DateTime.Now:ddMMyyyy_HHmmssff}.wav";
        }

        private bool Done(IReadOnlyCollection<float> buffer)
        {
            var containsOnlySilence = buffer.All(n => Math.Abs(n) < SilenceThreshold);

            if (containsOnlySilence)
            {
                _lengthOfSilenceInSamples += buffer.Count;
                var secondsOfSilence = _lengthOfSilenceInSamples/_writer.WaveFormat.SampleRate;
                return secondsOfSilence > 2;
            }

            _lengthOfSilenceInSamples = 0;
            return false;
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _writer.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
