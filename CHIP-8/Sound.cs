using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;

namespace CHIP_8
{
	public class Sound
	{
		private SoundPlayer BeepSoundPlayer; // TODO: Disposable

		public void Initialize()
		{
			// Initialize a WAV sine wave stream
			// (https://blogs.msdn.microsoft.com/ericlippert/2005/04/15/desafinado-part-four-rolling-your-own-wav-files/)
			using (var waveStream = new MemoryStream())
			using (var writer = new BinaryWriter(waveStream))
			{
				int RIFF = 0x46464952;
				int WAVE = 0x45564157;
				int formatChunkSize = 16;
				int headerSize = 8;
				int format = 0x20746D66;
				short formatType = 1;
				short tracks = 1;
				int samplesPerSecond = 44100;
				short bitsPerSample = 16;
				short frameSize = (short)(tracks * ((bitsPerSample + 7) / 8));
				int bytesPerSecond = samplesPerSecond * frameSize;
				int waveSize = 4;
				int data = 0x61746164;
				int samples = 147; // a whole multiple of (samples per second / freq) (aka 44100 / 600 == 73.5)
				int dataChunkSize = samples * frameSize;
				int fileSize = waveSize + headerSize + formatChunkSize + headerSize + dataChunkSize;
				writer.Write(RIFF);
				writer.Write(fileSize);
				writer.Write(WAVE);
				writer.Write(format);
				writer.Write(formatChunkSize);
				writer.Write(formatType);
				writer.Write(tracks);
				writer.Write(samplesPerSecond);
				writer.Write(bytesPerSecond);
				writer.Write(frameSize);
				writer.Write(bitsPerSample);
				writer.Write(data);
				writer.Write(dataChunkSize);
				double ampl = 5000;
				double freq = 600; // TODO: find a good frequency where you can't hear a "clip" when it stops (or maybe need some wave dampening when stopping)
				for (int i = 0; i < samples; i++)
				{
					double t = (double)i / (double)samplesPerSecond;
					short s = (short)(ampl * (Math.Sin(t * freq * 2.0 * Math.PI)));
					writer.Write(s);
				}

				waveStream.Position = 0;
				BeepSoundPlayer = new SoundPlayer(waveStream);
				BeepSoundPlayer.Load();
			}
		}

		public void StartPlaying()
		{
			BeepSoundPlayer.PlayLooping();
		}

		public void StopPlaying()
		{
			BeepSoundPlayer.Stop();
		}
	}
}
