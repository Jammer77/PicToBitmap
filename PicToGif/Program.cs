using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicToGif
{
	
	class Program
	{
		static void Main(string[] args)
		{
			var picFile = new PicFile("PLANET2.PIC");
			var data = picFile.GetIntRGBA();

		}
	}
}
