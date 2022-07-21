// Reading Adobe Swatch Exchange (ase) files using C#
// http://www.cyotek.com/blog/reading-adobe-swatch-exchange-ase-files-using-csharp

namespace AdobeSwatchExchangeLoader
{
  internal abstract class Block
  {
    #region Properties

    public byte[] ExtraData { get; set; }

    public string Name { get; set; }

    #endregion
  }
}
