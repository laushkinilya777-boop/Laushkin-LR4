using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace PhotoEditor.Core
{
    /// <summary>
    /// Фотография: имя, размеры и пиксели.
    /// Каждый пиксель хранится как одно число int (цвет в формате ARGB),
    /// пиксели идут построчно: индекс пикселя (x, y) = y * Width + x.
    /// </summary>
    public class Picture
    {
        /// <summary>Имя фотографии.</summary>
        public string Name { get; set; }

        /// <summary>Ширина в пикселях.</summary>
        public int Width { get; set; }

        /// <summary>Высота в пикселях.</summary>
        public int Height { get; set; }

        /// <summary>Пиксели (цвета в формате ARGB).</summary>
        public int[] Pixels { get; set; }

        /// <summary>Пустой конструктор — нужен для загрузки из JSON.</summary>
        public Picture() { }

        /// <summary>Создаёт белую картинку заданного размера.</summary>
        public Picture(string name, int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Ширина и высота должны быть больше нуля.");
            Name = name;
            Width = width;
            Height = height;
            Pixels = new int[width * height];
            for (int i = 0; i < Pixels.Length; i++)
                Pixels[i] = Color.White.ToArgb();
        }

        /// <summary>Возвращает цвет пикселя.</summary>
        public Color GetPixel(int x, int y)
        {
            CheckPoint(x, y);
            return Color.FromArgb(Pixels[y * Width + x]);
        }

        /// <summary>Меняет цвет одного пикселя.</summary>
        public void SetPixel(int x, int y, Color color)
        {
            CheckPoint(x, y);
            Pixels[y * Width + x] = color.ToArgb();
        }

        private void CheckPoint(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
                throw new ArgumentException($"Точка ({x}, {y}) за пределами картинки {Width}x{Height}.");
        }

        /// <summary>Обрезает картинку: оставляет прямоугольник (x, y, width, height).</summary>
        public void Crop(int x, int y, int width, int height)
        {
            if (width <= 0 || height <= 0 || x < 0 || y < 0 || x + width > Width || y + height > Height)
                throw new ArgumentException("Область обрезки выходит за пределы картинки.");

            int[] result = new int[width * height];
            for (int row = 0; row < height; row++)
                for (int col = 0; col < width; col++)
                    result[row * width + col] = Pixels[(y + row) * Width + (x + col)];

            Pixels = result;
            Width = width;
            Height = height;
        }

        /// <summary>Поворачивает картинку на 90° по часовой стрелке.</summary>
        public void RotateRight()
        {
            int newWidth = Height, newHeight = Width;
            int[] result = new int[Pixels.Length];
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    // пиксель (x, y) переходит в (Height - 1 - y, x)
                    int newX = Height - 1 - y, newY = x;
                    result[newY * newWidth + newX] = Pixels[y * Width + x];
                }

            Pixels = result;
            Width = newWidth;
            Height = newHeight;
        }

        /// <summary>Применяет фильтр ко всем пикселям.</summary>
        public void ApplyFilter(IFilter filter)
        {
            for (int i = 0; i < Pixels.Length; i++)
                Pixels[i] = filter.Apply(Color.FromArgb(Pixels[i])).ToArgb();
        }

        /// <summary>Пишет на картинке текст.</summary>
        public void AddText(string text, int x, int y, Color color, int fontSize)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Текст не может быть пустым.");
            using (Bitmap bitmap = ToBitmap())
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                using (Font font = new Font("Arial", fontSize, FontStyle.Bold))
                using (Brush brush = new SolidBrush(color))
                {
                    g.DrawString(text, font, brush, x, y);
                }
                CopyFrom(bitmap);
            }
        }

        /// <summary>
        /// Коллаж: ставит две картинки рядом (слева first, справа second) и возвращает новую картинку.
        /// </summary>
        public static Picture MakeCollage(Picture first, Picture second)
        {
            Picture result = new Picture("Коллаж", first.Width + second.Width, Math.Max(first.Height, second.Height));
            result.Paste(first, 0, 0);
            result.Paste(second, first.Width, 0);
            return result;
        }

        /// <summary>Вставляет другую картинку в точку (left, top).</summary>
        public void Paste(Picture other, int left, int top)
        {
            for (int y = 0; y < other.Height; y++)
                for (int x = 0; x < other.Width; x++)
                    if (left + x < Width && top + y < Height)
                        Pixels[(top + y) * Width + left + x] = other.Pixels[y * other.Width + x];
        }

        /// <summary>Копия картинки.</summary>
        public Picture Clone()
        {
            return new Picture { Name = Name, Width = Width, Height = Height, Pixels = (int[])Pixels.Clone() };
        }

        // ---------- преобразование в Bitmap и обратно (для файлов и надписей) ----------

        /// <summary>Создаёт Bitmap с теми же пикселями.</summary>
        public Bitmap ToBitmap()
        {
            Bitmap bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    bitmap.SetPixel(x, y, Color.FromArgb(Pixels[y * Width + x]));
            return bitmap;
        }

        private void CopyFrom(Bitmap bitmap)
        {
            Width = bitmap.Width;
            Height = bitmap.Height;
            Pixels = new int[Width * Height];
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    Pixels[y * Width + x] = bitmap.GetPixel(x, y).ToArgb();
        }

        /// <summary>Наибольшая сторона картинки при загрузке (большие фото уменьшаются).</summary>
        public const int MaxSize = 800;

        /// <summary>Загружает картинку из файла (jpg, png, bmp). Большие фото уменьшаются до MaxSize.</summary>
        public static Picture Load(string path)
        {
            using (Bitmap original = new Bitmap(path))
            {
                double k = Math.Min(1.0, (double)MaxSize / Math.Max(original.Width, original.Height));
                int w = Math.Max(1, (int)(original.Width * k)), h = Math.Max(1, (int)(original.Height * k));
                using (Bitmap bitmap = new Bitmap(original, w, h))
                {
                    Picture picture = new Picture { Name = System.IO.Path.GetFileName(path) };
                    picture.CopyFrom(bitmap);
                    return picture;
                }
            }
        }

        /// <summary>Сохраняет картинку в файл PNG.</summary>
        public void Save(string path)
        {
            using (Bitmap bitmap = ToBitmap())
                bitmap.Save(path, ImageFormat.Png);
        }

        public override string ToString() => $"{Name} ({Width}x{Height})";
    }
}
