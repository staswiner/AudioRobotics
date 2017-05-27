using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace winformsAudio
{
	class SoundEffect : WaveStream
	{
		public static Form1 form1 { get; set; }
		public WaveStream SourceStream { get; set; }
		public SoundEffect(WaveStream stream)
		{
			this.SourceStream = stream;
		}
		public override long Length
		{
			get
			{
				return SourceStream.Length;
			}
		}
		public override long Position
		{
			get
			{
				return SourceStream.Position;
			}

			set
			{
				SourceStream.Position = value;
			}
		}
		public override WaveFormat WaveFormat
		{
			get
			{
				return SourceStream.WaveFormat;
			}
		}
		public override int Read(byte[] buffer, int offset, int count)
		{
			int read = SourceStream.Read(buffer, offset, count);

			float multiply_factor = 2.0f;
			int multiply_i = 0;
			form1.CurrentFloatBuffer = buffer;
			form1.Invalidate();
			for (int i = 0; i < read / 2 - 6; i++, multiply_i++)
			{
				//if (multiply_i>multiply_factor)
				//{
				//	i--;
				//	multiply_i -= (int)multiply_factor;
				//	continue;
				//}
				// 0 1 4 5
				byte sound1 = buffer[i + 2];
				byte sound2 = buffer[i + 3];
				byte sound3 = buffer[i + 6];
				byte sound4 = buffer[i + 7];
				byte[] sound = { sound1, sound2, sound3, sound4 };
				float value = BitConverter.ToSingle(sound, 0) * 2.0f;


				byte[] bytes = BitConverter.GetBytes(value);

				buffer[i + 2] = bytes[0];
				buffer[i + 3] = bytes[1];
				buffer[i + 6] = bytes[2];
				buffer[i + 7] = bytes[3];

			}
			
			return read;
		}
	}
}
