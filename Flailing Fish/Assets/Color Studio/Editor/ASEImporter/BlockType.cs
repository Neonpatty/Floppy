namespace AdobeSwatchExchangeLoader
{
  // Reading Adobe Swatch Exchange (ase) files using C#
  // http://www.cyotek.com/blog/reading-adobe-swatch-exchange-ase-files-using-csharp

  internal enum BlockType
  {
    Color = 0x0001,

    GroupStart = 0xc001,

    GroupEnd = 0xc002
  }
}
