using System.Collections;
using System.Collections.Generic;

// Reading Adobe Swatch Exchange (ase) files using C#
// http://www.cyotek.com/blog/reading-adobe-swatch-exchange-ase-files-using-csharp

namespace AdobeSwatchExchangeLoader
{
  internal class ColorGroup : Block, IEnumerable<ColorEntry>
  {
    #region Constructors

    public ColorGroup()
    {
      this.Colors = new ColorEntryCollection();
    }

    #endregion

    #region Properties

    public ColorEntryCollection Colors { get; set; }

    #endregion

    #region IEnumerable<ColorEntry> Interface

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>
    /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
    /// </returns>
    public IEnumerator<ColorEntry> GetEnumerator()
    {
      return this.Colors.GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through a collection.
    /// </summary>
    /// <returns>
    /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
    /// </returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
      return this.GetEnumerator();
    }

    #endregion
  }
}
