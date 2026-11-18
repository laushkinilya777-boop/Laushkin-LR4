using System;
using System.Drawing;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PhotoEditor.Core;

namespace PhotoEditor.Tests
{
    [TestClass]
    public class PictureTests
    {
        [TestMethod]
        public void NewPicture_IsWhite()
        {
            Picture p = new Picture("test", 3, 2);
            Assert.AreEqual(Color.White.ToArgb(), p.GetPixel(2, 1).ToArgb());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void NewPicture_ZeroSize_Throws()
        {
            new Picture("test", 0, 5);
        }

        [TestMethod]
        public void SetPixel_ChangesColor()
        {
            Picture p = new Picture("test", 3, 3);
            p.SetPixel(1, 2, Color.Red);
            Assert.AreEqual(Color.Red.ToArgb(), p.GetPixel(1, 2).ToArgb());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SetPixel_OutOfPicture_Throws()
        {
            new Picture("test", 3, 3).SetPixel(5, 0, Color.Red);
        }

        [TestMethod]
        public void Crop_ChangesSizeAndKeepsPixels()
        {
            Picture p = new Picture("test", 10, 10);
            p.SetPixel(3, 4, Color.Red);
            p.Crop(2, 2, 5, 6);
            Assert.AreEqual(5, p.Width);
            Assert.AreEqual(6, p.Height);
            Assert.AreEqual(Color.Red.ToArgb(), p.GetPixel(1, 2).ToArgb());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Crop_TooBig_Throws()
        {
            new Picture("test", 10, 10).Crop(5, 5, 10, 10);
        }

        [TestMethod]
        public void RotateRight_SwapsSizeAndMovesPixel()
        {
            Picture p = new Picture("test", 4, 2);
            p.SetPixel(0, 0, Color.Red);   // левый верхний угол
            p.RotateRight();
            Assert.AreEqual(2, p.Width);
            Assert.AreEqual(4, p.Height);
            Assert.AreEqual(Color.Red.ToArgb(), p.GetPixel(1, 0).ToArgb()); // стал правым верхним
        }

        [TestMethod]
        public void RotateRight_FourTimes_ReturnsOriginal()
        {
            Picture p = new Picture("test", 4, 2);
            p.SetPixel(3, 1, Color.Blue);
            for (int i = 0; i < 4; i++) p.RotateRight();
            Assert.AreEqual(4, p.Width);
            Assert.AreEqual(Color.Blue.ToArgb(), p.GetPixel(3, 1).ToArgb());
        }

        [TestMethod]
        public void MakeCollage_PutsPicturesSideBySide()
        {
            Picture a = new Picture("a", 2, 2);
            Picture b = new Picture("b", 3, 4);
            b.SetPixel(0, 0, Color.Red);
            Picture c = Picture.MakeCollage(a, b);
            Assert.AreEqual(5, c.Width);
            Assert.AreEqual(4, c.Height);
            Assert.AreEqual(Color.Red.ToArgb(), c.GetPixel(2, 0).ToArgb());
        }

        [TestMethod]
        public void AddText_ChangesSomePixels()
        {
            Picture p = new Picture("test", 100, 40);
            p.AddText("Hi", 5, 5, Color.Black, 16);
            bool changed = false;
            foreach (int pixel in p.Pixels)
                if (pixel != Color.White.ToArgb()) changed = true;
            Assert.IsTrue(changed);
        }

        [TestMethod]
        public void SaveAndLoad_File_KeepsPixels()
        {
            string path = Path.Combine(Path.GetTempPath(), "picture_test.png");
            Picture p = new Picture("test", 5, 5);
            p.SetPixel(2, 2, Color.Green);
            p.Save(path);
            Picture loaded = Picture.Load(path);
            File.Delete(path);
            Assert.AreEqual(5, loaded.Width);
            Assert.AreEqual(Color.Green.ToArgb(), loaded.GetPixel(2, 2).ToArgb());
        }
    }

    [TestClass]
    public class FilterTests
    {
        [TestMethod]
        public void Brightness_AddsAndLimits()
        {
            Color c = new BrightnessFilter(100).Apply(Color.FromArgb(10, 200, 50));
            Assert.AreEqual(110, c.R);
            Assert.AreEqual(255, c.G); // не больше 255
            Assert.AreEqual(150, c.B);
        }

        [TestMethod]
        public void Grayscale_MakesEqualChannels()
        {
            Color c = new GrayscaleFilter().Apply(Color.FromArgb(30, 60, 90));
            Assert.AreEqual(60, c.R);
            Assert.AreEqual(60, c.G);
            Assert.AreEqual(60, c.B);
        }

        [TestMethod]
        public void Invert_Works()
        {
            Color c = new InvertFilter().Apply(Color.FromArgb(0, 100, 255));
            Assert.AreEqual(255, c.R);
            Assert.AreEqual(155, c.G);
            Assert.AreEqual(0, c.B);
        }

        [TestMethod]
        public void ApplyFilter_ChangesAllPixels()
        {
            Picture p = new Picture("test", 2, 2);
            p.ApplyFilter(new InvertFilter());
            Assert.AreEqual(Color.Black.ToArgb(), p.GetPixel(1, 1).ToArgb());
        }
    }

    [TestClass]
    public class AlbumTests
    {
        [TestMethod]
        public void Add_SelectsNewPhoto()
        {
            PhotoAlbum album = new PhotoAlbum();
            album.Add(new Picture("a", 1, 1));
            album.Add(new Picture("b", 1, 1));
            Assert.AreEqual("b", album.GetSelected().Name);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void GetSelected_EmptyAlbum_Throws()
        {
            new PhotoAlbum().GetSelected();
        }

        [TestMethod]
        public void SaveAndLoad_Json_RestoresState()
        {
            string path = Path.Combine(Path.GetTempPath(), "album_test.json");
            PhotoAlbum album = new PhotoAlbum();
            Picture p = new Picture("фото", 3, 3);
            p.SetPixel(1, 1, Color.Red);
            album.Add(p);
            album.Add(new Picture("второе", 2, 2));
            album.Select(0);

            IAlbumStorage storage = new JsonAlbumStorage();
            storage.Save(album, path);
            PhotoAlbum loaded = storage.Load(path);
            File.Delete(path);

            Assert.AreEqual(2, loaded.Photos.Count);
            Assert.AreEqual(0, loaded.SelectedIndex);
            Assert.AreEqual("фото", loaded.GetSelected().Name);
            Assert.AreEqual(Color.Red.ToArgb(), loaded.GetSelected().GetPixel(1, 1).ToArgb());
        }
    }
}
