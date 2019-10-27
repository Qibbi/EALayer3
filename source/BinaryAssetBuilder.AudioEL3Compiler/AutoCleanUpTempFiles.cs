using BinaryAssetBuilder.Core;
using System;
using System.IO;

namespace BinaryAssetBuilder.AudioEL3Compiler
{
    internal class AutoCleanUpTempFiles : IDisposable
    {
        private string _baseTempFileName;

        public AutoCleanUpTempFiles(string baseTempFileName)
        {
            _baseTempFileName = baseTempFileName;
        }

        public void Dispose()
        {
            Tracer tracer = Tracer.GetTracer(nameof(AudioEL3Compiler), "Provides Audio processing functionality.");
            foreach (string file in Directory.GetFiles(Path.GetDirectoryName(_baseTempFileName), Path.GetFileName(_baseTempFileName) + "*"))
            {
                tracer.TraceNote("Cleaning up stale temporary file {0}", file);
                try
                {
                    File.Delete(file);
                }
                catch (SystemException ex)
                {
                    tracer.TraceWarning("Could not delete stale temporary file {0}: {1}", file, ex);
                }
            }
        }
    }
}
