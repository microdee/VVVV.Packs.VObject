﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace VVVV.Packs.VObject
{
    // class for high level object management in VVVV
    public class VObjectDictionary
    {
        public Dictionary<string, VObjectCollectionWrap> Objects = new Dictionary<string, VObjectCollectionWrap>();
        public List<string> RemoveList = new List<string>();
        public VObjectDictionary() { }

        public void RemoveObject(string k)
        {
            VObjectCollection tbr = this.Objects[k].Content as VObjectCollection;
            tbr.Remove = true;
        }
        public void RemoveTagged()
        {
            foreach (string k in this.RemoveList)
            {
                this.Objects[k].Dispose();
                this.Objects.Remove(k);
            }
            this.RemoveList.Clear();
        }
        public void Clear()
        {
            foreach (KeyValuePair<string, VObjectCollectionWrap> kvp in this.Objects) kvp.Value.Dispose();
            this.Objects.Clear();
        }
    }
    public class VObjectDictionaryWrap : VObject
    {
        protected override void Dispose(bool disposing)
        {
            if (this.disposed)
                return;
            if (disposing)
            {
                VObjectDictionary ThisContent = this.Content as VObjectDictionary;
                ThisContent.Clear();
            }
            disposed = true;
        }
        public override Stream Serialize()
        {
            VObjectDictionary ThisContent = this.Content as VObjectDictionary;
            Stream dest = this.Serialized;
            dest.SetLength(0);
            dest.Position = 0;

            dest.WriteUint((uint)ThisContent.Objects.Count); // 0 | 4

            foreach (KeyValuePair<string, VObjectCollectionWrap> kvp in ThisContent.Objects) // 4 | CC*4
            {
                uint l = (uint)kvp.Value.Serialize().Length; // serialized here
                l += kvp.Key.UnicodeLength() + 4;
                dest.WriteUint(l);
            }

            foreach (KeyValuePair<string, VObjectCollectionWrap> kvp in ThisContent.Objects) // 4 + CC*4
            {
                dest.WriteUint(kvp.Key.UnicodeLength()); // 0 | 4
                dest.WriteUnicode(kvp.Key); // 4 | KL

                kvp.Value.Serialized.CopyTo(dest); // 4 + KL | CL // using the stream created above
            }
            return dest;
        }
        protected override void DeSerialize(Stream Input)
        {
            base.DeSerialize(Input);
            VObjectDictionary ThisContent = new VObjectDictionary();

            uint Count = this.Serialized.ReadUint();

            List<uint> ChildrenLengths = new List<uint>();
            for (int i = 0; i < Count; i++)
            {
                ChildrenLengths.Add(this.Serialized.ReadUint());
            }

            for (int i = 0; i < Count; i++)
            {
                uint keylength = this.Serialized.ReadUint();
                string keyname = this.Serialized.ReadUnicode((int)keylength);

                uint l = ChildrenLengths[i] - keylength - 4;
                Stream vobject = new MemoryStream();
                this.Serialized.CopyTo(vobject, (int)l);
                ThisContent.Objects.Add(keyname, new VObjectCollectionWrap(vobject));
            }
            this.Content = ThisContent;
        }
    }
}
