﻿using System;
using System.IO;
using System.Threading.Tasks;
using VGAudio.Containers.Wave;
using VGAudio.Formats;

namespace LoopingAudioConverter {
	public abstract class VGAudioExporter : IAudioExporter {
		protected abstract byte[] GetData(AudioData audio);
		protected abstract string GetExtension();

		private static AudioData Read(PCM16Audio lwav) {
			try {
				if (lwav.OriginalPath != null) {
					return VGAudioImporter.Read(File.ReadAllBytes(lwav.OriginalPath), lwav.OriginalPath);
				}
			} catch (NotImplementedException) { }

			return new WaveReader().Read(lwav.Export());
		}

		public void WriteFile(PCM16Audio lwav, string output_dir, string original_filename_no_ext) {
			AudioData audio = Read(lwav);
			audio.SetLoop(lwav.Looping, lwav.LoopStart, lwav.LoopEnd);
			byte[] data = GetData(audio);
			File.WriteAllBytes(Path.Combine(output_dir, original_filename_no_ext + GetExtension()), data);
		}

		public Task WriteFileAsync(PCM16Audio lwav, string output_dir, string original_filename_no_ext) {
			Task task = new Task(() => WriteFile(lwav, output_dir, original_filename_no_ext));
			task.Start();
			return task;
		}
	}
}
