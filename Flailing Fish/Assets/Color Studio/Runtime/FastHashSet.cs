using System;
using System.Collections.Generic;

namespace ColorStudio {

    public class FastHashSet<V> {
        int[] hashes;
        public DictionaryEntry[] entries;
        readonly int initialsize = 89;
        int nextfree;
        static readonly int[] primeSizes = new int[] { 89, 179, 359, 719, 1439, 2879, 5779, 11579, 23159, 46327,
            92657, 185323, 370661, 741337, 1482707, 2965421, 5930887, 11861791,
            23723599, 47447201, 94894427, 189788857, 379577741, 759155483
        };

        public struct DictionaryEntry {
            public int key;
            public int next;
            public V value;
        }


        public FastHashSet() {
            Initialize();
        }

        public FastHashSet(int capacity) {
            initialsize = FindNewSize(capacity);
            Initialize();
        }

        public int Add(object keyObj, V value, bool overwrite = true) {
            return Add(keyObj.GetHashCode(), value, overwrite);
        }

        public int Add(int key, V value, bool overwrite = true) {

            if (nextfree >= entries.Length) {
                Resize();
            }


            key &= 0x7FFFFFFF;

            int hashPos = key % hashes.Length;

            int entryLocation = hashes[hashPos];

            int storePos = nextfree;


            if (entryLocation != -1) { // already there
                int currEntryPos = entryLocation;

                do {
                    DictionaryEntry entry = entries[currEntryPos];

                    // same key is in the dictionary
                    if (key == entry.key) {
                        if (overwrite) {
                            entries[currEntryPos].value = value;
                        }
                        return currEntryPos;
                    }

                    currEntryPos = entry.next;
                } while (currEntryPos > -1);
            }

            // new value
            hashes[hashPos] = storePos;

            entries[storePos].next = entryLocation;
            entries[storePos].key = key;
            entries[storePos].value = value;

            nextfree++;

            return storePos;
        }


        private void Resize() {
            int newSize = FindNewSize(hashes.Length * 2 + 1);
            int[] newHashes = new int[newSize];
            DictionaryEntry[] newEntries = new DictionaryEntry[newSize];

            Array.Copy(entries, newEntries, nextfree);

            for (int i = 0; i < newSize; i++) {
                newHashes[i] = -1;
            }
            for (int i = nextfree; i < newSize; i++) {
                newEntries[i].key = -1;
            }

            for (int i = 0; i < nextfree; i++) {
                int key = newEntries[i].key;
                if (key >= 0) {
                    int hashPos = key % newSize;
                    int curPos = newHashes[hashPos];
                    newHashes[hashPos] = i;
                    newEntries[i].next = curPos;
                }
            }

            hashes = newHashes;
            entries = newEntries;
        }

        private int FindNewSize(int desiredCapacity) {
            for (int i = 0; i < primeSizes.Length; i++) {
                if (primeSizes[i] >= desiredCapacity)
                    return primeSizes[i];
            }

            throw new NotImplementedException("Too large array");
        }

        public V Get(int key) {
            int pos = GetPosition(key);

            if (pos == -1)
                throw new Exception("Key does not exist");

            return entries[pos].value;
        }

        public int GetPosition(int key) {
            key &= 0x7FFFFFFF;

            int pos = key % hashes.Length;

            int entryLocation = hashes[pos];

            if (entryLocation == -1)
                return -1;

            int nextpos = entryLocation;

            do {
                DictionaryEntry entry = entries[nextpos];

                if (key == entry.key)
                    return nextpos;

                nextpos = entry.next;

            } while (nextpos != -1);

            return -1;
        }

        public bool ContainsKey(int key) {
            return GetPosition(key) != -1;
        }

        public bool TryGetValue(int key, out V value) {
            int pos = GetPosition(key);

            if (pos == -1) {
                value = default(V);
                return false;
            }

            value = entries[pos].value;

            return true;
        }

        public V this[int key] {
            get {
                return Get(key);
            }
            set {
                Add(key, value, true);
            }
        }

        public void Add(KeyValuePair<int, V> item) {
            Add(item.Key, item.Value, false);
        }

        public void Clear() {
            nextfree = 0;
            for (int i = 0; i < hashes.Length; i++) {
                hashes[i] = -1;
            }
        }

        private void Initialize() {
            this.hashes = new int[initialsize];
            this.entries = new DictionaryEntry[initialsize];
            nextfree = 0;

            for (int i = 0; i < entries.Length; i++) {
                hashes[i] = -1;
                entries[i].key = -1;
            }
        }

        public int Count {
            get { return nextfree; }
        }

        public bool IsReadOnly {
            get { return false; }
        }


        public void Remove(object keyObj) {
            uint key = (uint)keyObj.GetHashCode();
            Remove((int)key);
        }


        public void Remove(int key) {
            key &= 0x7FFFFFFF;
            int hashPos = key % hashes.Length;

            int entryLocation = hashes[hashPos];

            if (entryLocation == -1)
                return;


            int currEntryPos = entryLocation;
            int prevEntryPos = entryLocation;

            do {
                DictionaryEntry entry = entries[currEntryPos];

                // key is in the dictionary
                if (key == entry.key) {
                    entries[currEntryPos].key = -1;
                    entries[prevEntryPos].next = entries[currEntryPos].next;
                    if (entryLocation == currEntryPos) {
                        hashes[hashPos] = entry.next;
                        if (entryLocation + 1 == nextfree) {
                            nextfree--;
                        }
                    }
                    return;
                }

                prevEntryPos = currEntryPos;
                currEntryPos = entry.next;

            } while (currEntryPos > -1);
        }


        public List<V> Values {
            get {
                List<V> values = new List<V>();
                for (int k = 0; k < hashes.Length; k++) {
                    int storePos = hashes[k];
                    if (storePos >= 0) {
                        DictionaryEntry entry = entries[storePos];
                        values.Add(entry.value);
                    }
                }
                return values;
            }
        }


    }

}