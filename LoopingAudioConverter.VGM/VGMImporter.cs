﻿using LoopingAudioConverter.FFmpeg;
using LoopingAudioConverter.PCM;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace LoopingAudioConverter.VGM {
	/// <summary>
	/// A class to use ffmpeg to render VGM/VGZ files to WAV format.
	/// </summary>
	public class VGMImporter : IAudioImporter {
		private readonly FFmpegEngine Engine;

		/// <summary>
		/// Initializes the VGM importer.
		/// </summary>
		/// <param name="engine">The FFmpeg handler</param>
		public VGMImporter(FFmpegEngine engine) {
			Engine = engine;
		}

		/// <summary>
		/// Returns whether the extension matches that of a VGM file (vgm or vgz), ignoring any leading period.
		/// </summary>
		/// <param name="extension">Filename extension</param>
		/// <returns>true if vgm or vgz, false otherwise</returns>
		public bool SupportsExtension(string extension) {
			while (extension.StartsWith(".")) extension = extension.Substring(1);
			return string.Equals(extension, "vgm", StringComparison.InvariantCultureIgnoreCase)
				|| string.Equals(extension, "vgz", StringComparison.InvariantCultureIgnoreCase);
		}

		bool IAudioImporter.SharesCodecsWith(IAudioExporter exporter) => false;

        private class Hints : IAudioHints {
            public int? SampleRate { get; set; }
			public int? SampleCount { get; set; }
		}

        /// <summary>
        /// Renders a file to WAV using VGMPlay and reads it into a PCM16Audio object.
        /// </summary>
        /// <param name="filename">The path of the file to read</param>
        /// <param name="preferredSampleRate">The sample rate to render the VGM at</param>
        /// <returns>A PCM16Audio, which may or may not be looping</returns>
        public async Task<PCM16Audio> ReadFileAsync(string filename, IAudioHints hints, IProgress<double> progress) {
			try {
				// Check format
				bool compressed;
				using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
				using (var br = new BinaryReader(fs)) {
					int tag = br.ReadUInt16();
					compressed = tag == 0x8B1F;
				}

				int samples, loopSamples;

				// Read loop points from file
				using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
				using (var gz = compressed ? new GZipStream(fs, CompressionMode.Decompress) : fs as Stream)
				using (var br = new BinaryReader(gz)) {
					int tag = br.ReadInt32();
					if (tag != 0x206D6756) throw new Exception($"File not in Vgm format ({tag:X8})");

					for (int i = 0; i < 5; i++) br.ReadInt32();

					samples = br.ReadInt32();
					br.ReadInt32();
					loopSamples = br.ReadInt32();
				}

				int r = hints.SampleRate ?? 44100;
				var data = await Engine.ReadFileAsync(filename, new Hints {
					SampleRate = r,
					SampleCount = samples
				}, progress);
				if (loopSamples != 0) {
					data.Looping = true;
					data.LoopStart = (int)Math.Round((samples - loopSamples) * (r / 44100.0));
					data.LoopEnd = (int)Math.Round(samples * (r / 44100.0));
				}
				return data;
			} catch (Exception e) when (!(e is AudioImporterException)) {
				throw new AudioImporterException("Could not read VGM: " + e.Message, e);
			}
		}
	}
}
