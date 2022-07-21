using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Reading Adobe Swatch Exchange (ase) files using C#
// http://www.cyotek.com/blog/reading-adobe-swatch-exchange-ase-files-using-csharp

namespace AdobeSwatchExchangeLoader {
    internal class SwatchExchangeData {
        #region Constructors

        public SwatchExchangeData() {
            this.Groups = new ColorGroupCollection();
            this.Colors = new ColorEntryCollection();
        }

        #endregion

        #region Properties

        public ColorEntryCollection Colors { get; set; }

        public ColorGroupCollection Groups { get; set; }

        #endregion

        #region Methods

        public void Load(string fileName) {
            using (Stream stream = File.OpenRead(fileName)) {
                this.Load(stream);
            }
        }

        public void Load(Stream stream) {
            Stack<ColorEntryCollection> colors = new Stack<ColorEntryCollection>();
            ColorGroupCollection groups = new ColorGroupCollection();
            ColorEntryCollection globalColors = new ColorEntryCollection();

            // add the global collection to the bottom of the stack to handle color blocks outside of a group
            colors.Push(globalColors);

            int blockCount;

            this.ReadAndValidateVersion(stream);

            blockCount = stream.ReadUInt32BigEndian();

            for (int i = 0; i < blockCount; i++) {
                this.ReadBlock(stream, groups, colors);
            }

            this.Groups = groups;
            this.Colors = globalColors;
        }

        public Color[] GetColors() {
            if (Colors == null) return new Color[0];
            List<Color> colors = new List<Color>();
            {
                for (int k = 0; k < Colors.Count; k++) {
                    colors.Add(new Color(Colors[k].R / 255f, Colors[k].G / 255f, Colors[k].B / 255f));
                }
                for (int g = 0; g < Groups.Count; g++) {
                    ColorGroup group = Groups[g];

                    for (int k = 0; k < group.Colors.Count; k++) {
                        colors.Add(new Color(group.Colors[k].R / 255f, group.Colors[k].G / 255f, group.Colors[k].B / 255f));
                    }
                }
                return colors.ToArray();
            }
        }


        private void ReadAndValidateVersion(Stream stream) {
            string signature;
            int majorVersion;
            int minorVersion;

            // get the signature (4 ascii characters)
            signature = stream.ReadAsciiString(4);

            if (signature != "ASEF") {
                throw new InvalidDataException("Invalid file format.");
            }

            // read the version
            majorVersion = stream.ReadUInt16BigEndian();
            minorVersion = stream.ReadUInt16BigEndian();

            if (majorVersion != 1 && minorVersion != 0) {
                throw new InvalidDataException("Invalid version information.");
            }
        }

        private void ReadBlock(Stream stream, ColorGroupCollection groups, Stack<ColorEntryCollection> colorStack) {
            BlockType blockType;
            int blockLength;
            int offset;
            int dataLength;
            Block block;

            blockType = (BlockType)stream.ReadUInt16BigEndian();

            blockLength = stream.ReadUInt32BigEndian();

            // store the current position of the stream, so we can calculate the offset
            // from bytes read to the block length in order to skip the bits we didn't
            // read, support or know what they are
            offset = (int)stream.Position;

            // process the actual block
            switch (blockType) {
                case BlockType.Color:
                    block = this.ReadColorBlock(stream, colorStack);
                    break;
                case BlockType.GroupStart:
                    block = this.ReadGroupBlock(stream, groups, colorStack);
                    break;
                case BlockType.GroupEnd:
                    block = null;
                    colorStack.Pop();
                    break;
                default:
                    throw new InvalidDataException("Unsupported block type " + blockType + ".");
            }

            // load in any custom data and attach it to the
            // current block (if available) as raw byte data
            dataLength = blockLength - (int)(stream.Position - offset);

            if (dataLength > 0) {
                byte[] extraData;

                extraData = new byte[dataLength];
                stream.Read(extraData, 0, dataLength);

                if (block != null) {
                    block.ExtraData = extraData;
                }
            }
        }

        private Block ReadColorBlock(Stream stream, Stack<ColorEntryCollection> colorStack) {
            ColorEntry block;
            string colorMode;
            int r;
            int g;
            int b;
            ColorType colorType;
            string name;
            ColorEntryCollection colors;

            // get the name of the color
            // this is stored as a null terminated string
            // with the length of the byte data stored before
            // the string data in a 16bit int
            name = stream.ReadStringBigEndian();

            // get the mode of the color, which is stored
            // as four ASCII characters
            colorMode = stream.ReadAsciiString(4);

            // read the color data
            // how much data we need to read depends on the
            // color mode we previously read
            switch (colorMode) {
                case "RGB ":
                    // RGB is comprised of three floating point values ranging from 0-1.0
                    float value1;
                    float value2;
                    float value3;
                    value1 = stream.ReadSingleBigEndian();
                    value2 = stream.ReadSingleBigEndian();
                    value3 = stream.ReadSingleBigEndian();
                    r = Convert.ToInt32(value1 * 255);
                    g = Convert.ToInt32(value2 * 255);
                    b = Convert.ToInt32(value3 * 255);
                    break;
                case "CMYK":
                    // CMYK is comprised of four floating point values
                    throw new InvalidDataException("Unsupported color mode " + colorMode + ".");
                case "LAB ":
                    // LAB is comprised of three floating point values
                    throw new InvalidDataException("Unsupported color mode " + colorMode + ".");
                case "Gray":
                    // Grayscale is comprised of a single floating point value
                    throw new InvalidDataException("Unsupported color mode " + colorMode + ".");
                default:
                    throw new InvalidDataException("Unsupported color mode " + colorMode + ".");
            }

            // the final "official" piece of data is a color type
            colorType = (ColorType)stream.ReadUInt16BigEndian();

            block = new ColorEntry {
                R = r,
                G = g,
                B = b,
                Name = name,
                Type = colorType
            };

            colors = colorStack.Peek();
            colors.Add(block);

            return block;
        }

        private Block ReadGroupBlock(Stream stream, ColorGroupCollection groups, Stack<ColorEntryCollection> colorStack) {
            ColorGroup block;
            string name;

            // read the name of the group
            name = stream.ReadStringBigEndian();

            // create the group and add it to the results set
            block = new ColorGroup {
                Name = name
            };

            groups.Add(block);

            // add the group color collection to the stack, so when subsequent color blocks
            // are read, they will be added to the correct collection
            colorStack.Push(block.Colors);

            return block;
        }

        #endregion
    }
}

