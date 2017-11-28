using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace PicToGif
{

	class Program
	{
		private static Bitmap CreateBitmapFromARGB(int width, int height, byte[] imageData)
		{
			var output = new Bitmap(width, height);
			var rect = new Rectangle(0, 0, output.Width, output.Height);
			BitmapData bitmapData = output.LockBits(rect, ImageLockMode.ReadWrite, output.PixelFormat);
			IntPtr ptr = bitmapData.Scan0;
			Marshal.Copy(imageData, 0, ptr, imageData.Length);
			output.UnlockBits(bitmapData);
			return output;
		}

		static void Main(string[] args)
		{
			if(!args.Any())
			{
				Console.Write("Give me file name");
				return;
			}
			var picFile = new PicFile(args.First());
			var data = picFile.GetByteRGBA().ToArray();
			var bitmap = CreateBitmapFromARGB(320, 200, data);
			var name = args.First().Split('.').First();
			bitmap.Save($"{name}.bmp");
		}
	}
}
