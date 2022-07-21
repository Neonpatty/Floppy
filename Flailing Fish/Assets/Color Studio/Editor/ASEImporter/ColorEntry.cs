using UnityEngine;

namespace AdobeSwatchExchangeLoader
{
  internal class ColorEntry : Block
  {
    #region Properties

    public int B { get; set; }

    public int G { get; set; }

    public int R { get; set; }

    public ColorType Type { get; set; }

    #endregion

    #region Methods

    public Color ToColor()
    {
      return new Color(this.R, this.G, this.B);
    }

    #endregion
  }
}
