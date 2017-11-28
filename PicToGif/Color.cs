using System;

namespace PicToGif
{
	public struct Color
	{
		public byte R { get; set; }
		public byte G { get; set; }
		public byte B { get; set; }
		public byte A { get; set; }

		public static Color Transparent
		{
			get
			{
				return new Color(0, 0, 0, 0);
			}
		}

		public static Color Black
		{
			get
			{
				return new Color(0, 0, 0);
			}
		}

		public static bool operator ==(Color a, Color b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(Color a, Color b)
		{
			return !a.Equals(b);
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Color)) return false;
			Color color = (Color)obj;
			return GetHashCode() == color.GetHashCode();
		}

		public override int GetHashCode()
		{
			return ((int)A << 24) + ((int)B << 16) + ((int)G << 8) + ((int)R);
		}

		public Color(byte a, byte r, byte g, byte b)
		{
			R = r;
			G = g;
			B = b;
			A = a;
		}

		public Color(byte a, int r, int g, int b)
		{
			R = (byte)r;
			G = (byte)g;
			B = (byte)b;
			A = (byte)a;
		}

		public Color(byte r, byte g, byte b)
		{
			R = r;
			G = g;
			B = b;
			A = 255;
		}

		public Color(int r, int g, int b)
		{
			R = (byte)r;
			G = (byte)g;
			B = (byte)b;
			A = 255;
		}

		public int GetInegerRGBA()
		{
			return MakeRGBAInteger(R, G, B, A);
		}

		public static int MakeRGBAInteger(byte r, byte g, byte b, byte a)
		{
			var bytes = new[] { r, g, b, a };
			int result = BitConverter.ToInt32(bytes, 0);
			return result;
		}
	}
}