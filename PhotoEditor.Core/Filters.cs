using System;
using System.Drawing;

namespace PhotoEditor.Core
{
    /// <summary>Фильтр: получает цвет пикселя и возвращает новый цвет.</summary>
    public interface IFilter
    {
        /// <summary>Название фильтра.</summary>
        string Name { get; }

        /// <summary>Обрабатывает цвет одного пикселя.</summary>
        Color Apply(Color color);
    }

    /// <summary>Осветление (amount &gt; 0) или затемнение (amount &lt; 0).</summary>
    public class BrightnessFilter : IFilter
    {
        private readonly int amount;

        public BrightnessFilter(int amount)
        {
            this.amount = amount;
        }

        public string Name => "Яркость " + amount;

        public Color Apply(Color c)
        {
            return Color.FromArgb(c.A, Limit(c.R + amount), Limit(c.G + amount), Limit(c.B + amount));
        }

        /// <summary>Ограничивает значение диапазоном 0..255.</summary>
        private static int Limit(int value) => Math.Max(0, Math.Min(255, value));
    }

    /// <summary>Чёрно-белое изображение (оттенки серого).</summary>
    public class GrayscaleFilter : IFilter
    {
        public string Name => "Чёрно-белый";

        public Color Apply(Color c)
        {
            int gray = (c.R + c.G + c.B) / 3;
            return Color.FromArgb(c.A, gray, gray, gray);
        }
    }

    /// <summary>Негатив (инверсия цветов).</summary>
    public class InvertFilter : IFilter
    {
        public string Name => "Негатив";

        public Color Apply(Color c)
        {
            return Color.FromArgb(c.A, 255 - c.R, 255 - c.G, 255 - c.B);
        }
    }

    /// <summary>Сепия — "старая фотография" (изменение цветов).</summary>
    public class SepiaFilter : IFilter
    {
        public string Name => "Сепия";

        public Color Apply(Color c)
        {
            int r = (int)(0.393 * c.R + 0.769 * c.G + 0.189 * c.B);
            int g = (int)(0.349 * c.R + 0.686 * c.G + 0.168 * c.B);
            int b = (int)(0.272 * c.R + 0.534 * c.G + 0.131 * c.B);
            return Color.FromArgb(c.A, Math.Min(255, r), Math.Min(255, g), Math.Min(255, b));
        }
    }
}
