//
// File.cs:
//
// Author:
//   Julien Moutte <julien@fluendo.com>
//   Sebastien Mouy <starwer@laposte.net>
//
// Copyright (C) 2011 FLUENDO S.A.
//
// This library is free software; you can redistribute it and/or modify
// it  under the terms of the GNU Lesser General Public License version
// 2.1 as published by the Free Software Foundation.
//
// This library is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307
// USA
//

using System.Collections.Generic;
using System;
using System.Linq;

namespace TagLib.Matroska
{
    /// <summary>
    /// Enumeration listing supported Matroska track types.
    /// </summary>
    public enum TrackType
    {
        /// <summary>
        /// Video track type.
        /// </summary>
        Video = 0x1,
        /// <summary>
        /// Audio track type.
        /// </summary>
        Audio = 0x2,
        /// <summary>
        /// Complex track type.
        /// </summary>
        Complex = 0x3,
        /// <summary>
        /// Logo track type.
        /// </summary>
        Logo = 0x10,
        /// <summary>
        /// Subtitle track type.
        /// </summary>
        Subtitle = 0x11,
        /// <summary>
        /// Buttons track type.
        /// </summary>
        Buttons = 0x12,
        /// <summary>
        /// Control track type.
        /// </summary>
        Control = 0x20
    }

    /// <summary>
    ///    This class extends <see cref="TagLib.File" /> to provide tagging
    ///    and properties support for Matroska files.
    /// </summary>
    [SupportedMimeType ("taglib/mkv", "mkv")]
    [SupportedMimeType ("taglib/mka", "mka")]
    [SupportedMimeType ("taglib/mks", "mks")]
    [SupportedMimeType ("video/webm")]
    [SupportedMimeType ("video/x-matroska")]
    public class File : TagLib.File
    {
        #region Private Fields

        /// <summary>
        ///   Contains the tags for the file.
        /// </summary>
        private Matroska.Tags tags;

        /// <summary>
        ///    Contains the media properties.
        /// </summary>
        private Properties properties;

        private double duration_unscaled;
        private ulong time_scale;
        private TimeSpan duration;

        private List<Track> tracks = new List<Track> ();

        private bool updateTags = false;

        #endregion

        
        #region Constructors

        /// <summary>
        ///    Constructs and initializes a new instance of <see
        ///    cref="File" /> for a specified path in the local file
        ///    system and specified read style.
        /// </summary>
        /// <param name="path">
        ///    A <see cref="string" /> object containing the path of the
        ///    file to use in the new instance.
        /// </param>
        /// <param name="propertiesStyle">
        ///    A <see cref="ReadStyle" /> value specifying at what level
        ///    of accuracy to read the media properties, or <see
        ///    cref="ReadStyle.None" /> to ignore the properties.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///    <paramref name="path" /> is <see langword="null" />.
        /// </exception>
        public File (string path, ReadStyle propertiesStyle)
            : this (new File.LocalFileAbstraction (path),
                propertiesStyle)
        {
        }

        /// <summary>
        ///    Constructs and initializes a new instance of <see
        ///    cref="File" /> for a specified path in the local file
        ///    system with an average read style.
        /// </summary>
        /// <param name="path">
        ///    A <see cref="string" /> object containing the path of the
        ///    file to use in the new instance.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///    <paramref name="path" /> is <see langword="null" />.
        /// </exception>
        public File (string path)
            : this (path, ReadStyle.Average)
        {
        }

        /// <summary>
        ///    Constructs and initializes a new instance of <see
        ///    cref="File" /> for a specified file abstraction and
        ///    specified read style.
        /// </summary>
        /// <param name="abstraction">
        ///    A <see cref="TagLib.File.IFileAbstraction" /> object to use when
        ///    reading from and writing to the file.
        /// </param>
        /// <param name="propertiesStyle">
        ///    A <see cref="ReadStyle" /> value specifying at what level
        ///    of accuracy to read the media properties, or <see
        ///    cref="ReadStyle.None" /> to ignore the properties.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///    <paramref name="abstraction" /> is <see langword="null"
        ///    />.
        /// </exception>
        public File (File.IFileAbstraction abstraction,
                     ReadStyle propertiesStyle)
            : base (abstraction)
        {
            tags = new Matroska.Tags(tracks);

            Mode = AccessMode.Read;

            try {
                ReadWrite (propertiesStyle);
                TagTypesOnDisk = TagTypes;
            }
            finally {
                Mode = AccessMode.Closed;
            }

            List<ICodec> codecs = new List<ICodec> ();

            foreach (Track track in tracks) {
                codecs.Add (track);
            }

            properties = new Properties (duration, codecs);

            updateTags = true;
        }

        /// <summary>
        ///    Constructs and initializes a new instance of <see
        ///    cref="File" /> for a specified file abstraction with an
        ///    average read style.
        /// </summary>
        /// <param name="abstraction">
        ///    A <see cref="TagLib.File.IFileAbstraction" /> object to use when
        ///    reading from and writing to the file.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///    <paramref name="abstraction" /> is <see langword="null"
        ///    />.
        /// </exception>
        public File (File.IFileAbstraction abstraction)
            : this (abstraction, ReadStyle.Average)
        {
        }

        #endregion

        
        #region Public Methods


        /// <summary>
        ///    Saves the changes made in the current instance to the
        ///    file it represents.
        /// </summary>
        public override void Save ()
        {
            Mode = AccessMode.Write;
            try {
                ReadWrite(ReadStyle.None);
            }
            finally {
                Mode = AccessMode.Closed;
            }
        }

        /// <summary>
        ///    Removes a set of tag types from the current instance.
        /// </summary>
        /// <param name="types">
        ///    A bitwise combined <see cref="TagLib.TagTypes" /> value
        ///    containing tag types to be removed from the file.
        /// </param>
        /// <remarks>
        ///    In order to remove all tags from a file, pass <see
        ///    cref="TagTypes.AllTags" /> as <paramref name="types" />.
        /// </remarks>
        public override void RemoveTags (TagLib.TagTypes types)
        {
            if((types & TagTypes.Matroska) !=0)
            {
                tags.Clear();
            }
        }

        /// <summary>
        ///    Gets a tag of a specified type from the current instance,
        ///    optionally creating a new tag if possible.
        /// </summary>
        /// <param name="type">
        ///    A <see cref="TagLib.TagTypes" /> value indicating the
        ///    type of tag to read.
        /// </param>
        /// <param name="create">
        ///    A <see cref="bool" /> value specifying whether or not to
        ///    try and create the tag if one is not found.
        /// </param>
        /// <returns>
        ///    A <see cref="Tag" /> object containing the tag that was
        ///    found in or added to the current instance. If no
        ///    matching tag was found and none was created, <see
        ///    langword="null" /> is returned.
        /// </returns>
        public override TagLib.Tag GetTag (TagLib.TagTypes type,
                                           bool create)
        {
            TagLib.Tag ret = null;
            if (type == TagTypes.Matroska) ret = Tag;
            return ret;
        }

        #endregion


        #region Public Properties

        /// <summary>
        ///    Gets a abstract representation of all tags stored in the
        ///    current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="TagLib.Tag" /> object representing all tags
        ///    stored in the current instance.
        /// </value>
        public override TagLib.Tag Tag
        {
            get
            {
                if(updateTags)
                {
                    var retag = tags.ToArray();
                    foreach (var tag in retag)
                    {
                        // This will force the default TagetTypeValue to get a proper value according to the medium type (audio/video)
                        if (tag.TargetTypeValue == 0) tags.Add(tag);
                    }
                    updateTags = false;
                }

                // Add Empty Tag representing the Medium to avoid null object
                if (tags.Medium == null)
                {
                    new Tag(tags); 
                }

                return tags.Medium;
            }
        }

        /// <summary>
        ///    Gets the media properties of the file represented by the
        ///    current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="TagLib.Properties" /> object containing the
        ///    media properties of the file represented by the current
        ///    instance.
        /// </value>
        public override TagLib.Properties Properties
        {
            get
            {
                return properties;
            }
        }

        #endregion


        #region Private Methods

        /// <summary>
        ///    Reads (and Write, if file Mode is Write) the file with a specified read style.
        /// </summary>
        /// <param name="propertiesStyle">
        ///    A <see cref="ReadStyle" /> value specifying at what level
        ///    of accuracy to read the media properties, or <see
        ///    cref="ReadStyle.None" /> to ignore the properties.
        /// </param>
        private void ReadWrite (ReadStyle propertiesStyle)
        {
            ulong offset = 0;

            while (offset < (ulong) Length) {
                EBMLElement element = new EBMLElement (this, offset);

                EBMLID ebml_id = (EBMLID) element.ID;
                MatroskaID matroska_id = (MatroskaID) element.ID;

                switch (ebml_id) {
                    case EBMLID.EBMLHeader:
                        ReadHeader (element);
                        break;
                    default:
                        break;
                }
                switch (matroska_id) {
                    case MatroskaID.Segment:
                        ReadWriteSegment(element, propertiesStyle);
                        break;
                    default:
                        break;
                }

                offset += element.Size;
            }
        }

        private void ReadHeader (EBMLElement element)
        {
            string doctype = null;
            ulong i = 0;

            while (i < element.DataSize) {
                EBMLElement child = new EBMLElement (this, element.DataOffset + i);

                EBMLID ebml_id = (EBMLID) child.ID;

                switch (ebml_id) {
                    case EBMLID.EBMLDocType:
                        doctype = child.ReadString ();
                        break;
                    default:
                        break;
                }

                i += child.Size;
            }

            // Check DocType
            if (String.IsNullOrEmpty (doctype) || (doctype != "matroska" && doctype != "webm")) {
                throw new UnsupportedFormatException ("DocType is not matroska or webm");
            }
        }

        private void ReadWriteSegment (EBMLElement element, ReadStyle propertiesStyle)
        {
            long ret = 0;
            ulong i = 0;
            var segm_list = new List<EBMLElement>(8);
            EBMLElement ebml_seek = null; // find first SeekHead
            ulong ebml_seek_end = 0;

            while (i < (ulong)((long)element.DataSize + ret)) {
                EBMLElement child = new EBMLElement (this, element.DataOffset + i);

                MatroskaID matroska_id = (MatroskaID) child.ID;
                bool seek = true;

                switch (matroska_id) {
                    case MatroskaID.SeekHead:
                        if (ebml_seek == null && i == 0)
                        {
                            ebml_seek = child;
                            ebml_seek_end = i + child.Size;
                        }
                        else if (Mode == AccessMode.Write)
                        {
                            ret += child.Remove(); // Remove all other SeekHeads
                            continue;
                        }
                        seek = false;
                        break;
                    case MatroskaID.Tracks:
                        if(propertiesStyle != ReadStyle.None) ReadTracks (child);
                        break;
                    case MatroskaID.SegmentInfo:
                        ret += ReadWriteSegmentInfo(child);
                        break;
                    case MatroskaID.Tags:
                        if (Mode == AccessMode.Write)
                        {
                            ret += child.Remove(); // Remove all Tags
                            continue;
                        }
                        ReadTags(child);
                        break;
                    case MatroskaID.Attachments:
                        if (Mode == AccessMode.Write)
                        {
                            ret += child.Remove(); // Remove all Tags
                            continue;
                        }
                        ReadAttachments(child);
                        break;
                    case MatroskaID.Cluster:
                        break;
                    case MatroskaID.Void:
                        if (ebml_seek_end == i) ebml_seek_end += child.Size; // take on void
                        seek = false;
                        break;
                    default:
                        break;
                }

                if(seek) segm_list.Add(child);

                i += child.Size;
            }

            if (Mode == AccessMode.Write)
            {
                i += element.DataOffset;

                // Create EBML Attachments
                if (tags.Attachments.Length > 0)
                {
                    var ebml_attach = new EBMLElement(this, i, MatroskaID.Attachments);
                    WriteAttachments(ebml_attach);
                    ret += (long)ebml_attach.Size;
                    i += ebml_attach.Size;
                    segm_list.Add(ebml_attach);
                }

                // Create EBML Tags if Tag available
                if (tags.Count > 0 && (tags.Count>1 || !tags[0].IsEmpty))
                {
                    var ebml_tags = new EBMLElement(this, i, MatroskaID.Tags);
                    WriteTags(ebml_tags);
                    ret += (long)ebml_tags.Size;
                    i += ebml_tags.Size;
                    segm_list.Add(ebml_tags);
                }

                // Generate the SeekHead
                ret += WriteSeekHead(element.DataOffset, (long)ebml_seek_end, segm_list);

                // Update Element EBML data-size
                ret = element.ResizeData(ret);
            }
            else
            {
                // Resolve the stub UIDElement to their real object (if available)
                foreach (var tag in tags)
                {
                    if (tag.Elements != null) {
                        for (int k = 0; k < tag.Elements.Count; k++)
                        {
                            var stub = tag.Elements[k];

                            // Attachments
                            if (tags.Attachments != null)
                            {
                                foreach (var obj in tags.Attachments)
                                {
                                    if (stub.UID == obj.UID && stub.UIDType == obj.UIDType)
                                        tag.Elements[k] = obj;
                                }
                            }


                            // Tracks
                            if (tracks != null)
                            {
                                foreach (var tobj in tracks)
                                {
                                    var obj = tobj as IUIDElement;
                                    if (obj != null)
                                    {
                                        if (stub.UID == obj.UID && stub.UIDType == obj.UIDType)
                                            tag.Elements[k] = tobj;
                                    }
                                }
                            }

                        }
                    }
                }

            }
        }


        private long  WriteSeekHead(ulong position, long oldsize = 0, List<EBMLElement> segm_list = null)
        {
            // Reserve size upfront for the SeekHead to know the offset to apply to each position
            long seek_size = 4 + 8 + (segm_list.Count * (4 + 8 + 2 * (2 + 8))) + 10;
            if (oldsize > seek_size) seek_size = oldsize;
            long ret = seek_size - oldsize;
            long offset = ret - (long)position;

            // Replace old SeekHead by new one
            if(oldsize>0) RemoveBlock((long)position, oldsize);
            var element = new EBMLElement(this, position, MatroskaID.SeekHead);
            position = element.DataOffset;


            // Create the Seek Entries
            foreach (var segm in segm_list)
            {
                ulong i = position;

                var seekEntry = new EBMLElement(this, i, MatroskaID.Seek);
                i += seekEntry.Size;

                var seekId = new EBMLElement(this, i, MatroskaID.SeekID);
                seekId.WriteData(segm.ID);
                i += seekId.Size;

                var seekPosition = new EBMLElement(this, i, MatroskaID.SeekPosition);
                seekPosition.WriteData((ulong)((long)segm.Offset + offset));
                i += seekPosition.Size;

                seekEntry.DataSize = seekId.Size + seekPosition.Size;
                position += seekEntry.Size;
            }


            // Update Element EBML data-size
            element.DataSize = position - element.DataOffset;

            // Complete with reserved size with void
            long voidSize = seek_size - (long)element.Size;
            WriteVoid(element.Offset + element.Size, (int)voidSize);

            return ret;
        }


        private void WriteVoid(ulong position, int size)
        {
            if (size<2) throw new UnsupportedFormatException("Invalid size for WriteVoid");

            var element = new EBMLElement(this, position, MatroskaID.Void);
            element.DataSize = (ulong)size;
            element.DataSize = 0;
            element.WriteData(new ByteVector(size - (int)element.Size));
        }


        private long ReadWriteSeek(EBMLElement element, long offset = 0, List<EBMLElement> segm_list = null)
        {
            long ret = 0;
            ulong i = 0;
            EBMLElement ebml_id = null;
            EBMLElement ebml_position = null;

            while (i < (ulong)((long)element.DataSize + ret))
            {
                EBMLElement child = new EBMLElement(this, element.DataOffset + i);

                MatroskaID matroska_id = (MatroskaID)child.ID;

                switch (matroska_id)
                {
                    case MatroskaID.SeekID:
                        ebml_id = child;
                        break;
                    case MatroskaID.SeekPosition:
                        ebml_position = child;
                        break;
                    default:
                        break;
                }

                i += child.Size;
            }

            if (Mode == AccessMode.Write)
            {
                i += element.DataOffset;

                if(ebml_id != null && ebml_position != null)
                {
                    // Valid
                    ulong target_id = ebml_id.ReadULong();
                    for(int idx=0; idx< segm_list.Count; idx++)
                    {
                        var ebml = segm_list[idx];
                        if (target_id == ebml.ID)
                        {
                            long position = (long) ebml.Offset + offset;
                            segm_list.RemoveAt(idx);
                            idx--;
                        }
                    }


                    // Update Element EBML data-size
                    ret = element.ResizeData(ret);
                }
                else
                {
                    // Invalid
                    RemoveBlock((long)i, (long)element.Size);
                }
            }

            return ret;
        }


        private void ReadTags (EBMLElement element)
        {
            ulong i = 0;

            while (i < (ulong)((long)element.DataSize)) {
                EBMLElement child = new EBMLElement (this, element.DataOffset + i);

                MatroskaID matroska_id = (MatroskaID) child.ID;

                switch (matroska_id) {
                    case MatroskaID.Tag:
                        ReadTag(child);
                        break;
                    default:
                        break;
                }

                i += child.Size;
            }
        }


        private void ReadTag(EBMLElement element)
        {
            ulong i = 0;

            // Create new Tag
            var tag = new Matroska.Tag(tags);

            while (i < (ulong)((long)element.DataSize))
            {
                EBMLElement child = new EBMLElement(this, element.DataOffset + i);

                MatroskaID matroska_id = (MatroskaID)child.ID;

                switch (matroska_id)
                {
                    case MatroskaID.Targets:
                        ReadTargets(child, tag);
                        break;
                    case MatroskaID.SimpleTag:
                        ReadSimpleTag(child, tag);
                        break;
                    default:
                        break;
                }

                i += child.Size;
            }

        }


        private void ReadTargets(EBMLElement element, Tag tag)
        {
            ulong i = 0;

            string targetType = null;
            var uids = new List<UIDElement>();

            while (i < element.DataSize)
            {
                EBMLElement child = new EBMLElement(this, element.DataOffset + i);

                MatroskaID matroska_id = (MatroskaID)child.ID;

                switch (matroska_id)
                {
                    case MatroskaID.TargetTypeValue:
                        tag.TargetTypeValue = (ushort)child.ReadULong();
                        break;
                    case MatroskaID.TargetType:
                        targetType = child.ReadString();
                        break;
                    case MatroskaID.TagTrackUID:
                    case MatroskaID.TagEditionUID:
                    case MatroskaID.TagChapterUID:
                    case MatroskaID.TagAttachmentUID:
                        var uid = child.ReadULong();
                        // Value 0 => apply to all
                        if (uid != 0) uids.Add( new UIDElement(matroska_id, uid) );
                        break;
                    default:
                        break;
                }

                i += child.Size;
            }

            tag.TargetType = targetType;
            if (uids.Count > 0)
            {
                tag.Elements = new List<IUIDElement>(uids.Count);
                tag.Elements.AddRange(uids);
            }
        }


        private void ReadSimpleTag(EBMLElement element, Tag tag, SimpleTag simpletag = null)
        {
            ulong i = 0;
            string key = null;
            var stag = new SimpleTag();

            while (i < (ulong)((long)element.DataSize))
            {
                EBMLElement child = new EBMLElement(this, element.DataOffset + i);

                MatroskaID matroska_id = (MatroskaID)child.ID;

                switch (matroska_id)
                {
                    case MatroskaID.TagName:
                        key = child.ReadString();
                        break;
                    case MatroskaID.TagLanguage:
                        stag.TagLanguage = child.ReadString();
                        break;
                    case MatroskaID.TagDefault:
                        stag.TagDefault = child.ReadULong() != 0;
                        break;
                    case MatroskaID.TagString:
                        stag.TagBinary = false;
                        stag.Value = child.ReadBytes();
                        break;
                    case MatroskaID.TagBinary:
                        stag.TagBinary = true;
                        stag.Value = child.ReadBytes();
                        break;
                    case MatroskaID.SimpleTag:
                        ReadSimpleTag(child, null, stag);
                        break;
                    default:
                        break;
                }

                i += child.Size;
            }

            // Add the SimpleTag reference to its parent
            if (key != null) 
            {
                key = key.ToUpper();

                List<SimpleTag> list = null;

                if (tag != null)
                {
                    if (tag.SimpleTags == null)
                        tag.SimpleTags = new Dictionary<string, List<SimpleTag>>(StringComparer.OrdinalIgnoreCase);
                    else
                        tag.SimpleTags.TryGetValue(key, out list);

                    if (list == null)
                        tag.SimpleTags[key] = list = new List<SimpleTag>(6);
                }
                else
                {
                    if (simpletag.SimpleTags == null)
                        simpletag.SimpleTags = new Dictionary<string, List<SimpleTag>>(StringComparer.OrdinalIgnoreCase);
                    else
                        simpletag.SimpleTags.TryGetValue(key, out list);

                    if (list == null)
                        simpletag.SimpleTags[key] = list = new List<SimpleTag>(1);
                }

                list.Add(stag);
            }

        }

        private void WriteTags(EBMLElement element)
        {
            ulong i = element.DataOffset;

            foreach (var tag in tags)
            {
                if (tag.SimpleTags != null && tag.SimpleTags.Count > 0)
                {
                    // Create new EBML Tag
                    var ebml_tag = new EBMLElement(this, i, MatroskaID.Tag);

                    // Write Tag content
                    WriteTag(ebml_tag, tag);
                    i += ebml_tag.Size;
                }
            }

            // Update Tags EBML data-size
            element.DataSize = i - element.DataOffset;

        }


        private void WriteTag(EBMLElement element, Tag tag)
        {
            ulong i = element.DataOffset;

            // Write Targets
            var ebml_targets = new EBMLElement(this, i, MatroskaID.Targets);
            WriteTargets(ebml_targets, tag);
            i += ebml_targets.Size;

            // Extract/Write the SimpleTag from the Tag object
            foreach( var stagList in tag.SimpleTags)
            {
                string key = stagList.Key;
                foreach (var stag in stagList.Value)
                {
                    var ebml_Simpletag = new EBMLElement(this, i, MatroskaID.SimpleTag);
                    WriteSimpleTag(ebml_Simpletag, key, stag);
                    i += ebml_Simpletag.Size;
                }
            }

            // Update Tag EBML data-size
            element.DataSize = i - element.DataOffset;
        }


        private void WriteSimpleTag (EBMLElement element, string key, SimpleTag value)
        {
            ulong i = element.DataOffset;

            key = key.ToUpper();

            // Write SimpleTag content
            var ebml_tagName = new EBMLElement(this, i, MatroskaID.TagName, key);
            i += ebml_tagName.Size;

            var ebml_tagLanguage = new EBMLElement(this, i, MatroskaID.TagLanguage, value.TagLanguage);
            i += ebml_tagLanguage.Size;

            var ebml_tagDefault = new EBMLElement(this, i, MatroskaID.TagDefault, value.TagDefault ? (ulong)1 : 0);
            i += ebml_tagDefault.Size;

            var ebml_tagValue = new EBMLElement(this, i, value.TagBinary ? MatroskaID.TagBinary : MatroskaID.TagString, value);
            i += ebml_tagValue.Size;

            // Nested SimpleTag (Recursion)
            if(value.SimpleTags != null)
            {
                foreach (var stagList in value.SimpleTags)
                {
                    foreach (var stag in stagList.Value)
                    {
                        var ebml_Simpletag = new EBMLElement(this, i, MatroskaID.SimpleTag);
                        WriteSimpleTag(ebml_Simpletag, stagList.Key, stag);
                        i += ebml_Simpletag.Size;
                    }
                }
            }

            // Update Tag EBML data-size
            element.DataSize = i - element.DataOffset;
        }


        private void WriteTargets(EBMLElement element, Tag tag)
        {
            ulong i = element.DataOffset;

            // Write Targets content

            if (tag.TargetTypeValue > 0) 
            {
                var ebml_targetTypeValue = new EBMLElement(this, i, MatroskaID.TargetTypeValue, tag.TargetTypeValue);
                i += ebml_targetTypeValue.Size;
            }

            if (tag.TargetType != null)
            {
                var ebml_targetType = new EBMLElement(this, i, MatroskaID.TargetType, tag.TargetType);
                i += ebml_targetType.Size;
            }

            if (tag.Elements != null)
            {
                foreach (var value in tag.Elements)
                {
                    var ebml_targetUID = new EBMLElement(this, i, value.UIDType, value.UID);
                    i += ebml_targetUID.Size;
                }
            }
            
            // Update Tag EBML data-size
            element.DataSize = i - element.DataOffset;
        }



        private void ReadAttachments(EBMLElement element)
        {
            ulong i = 0;

            while (i < (ulong)((long)element.DataSize))
            {
                EBMLElement child = new EBMLElement(this, element.DataOffset + i);

                MatroskaID matroska_id = (MatroskaID)child.ID;

                switch (matroska_id)
                {
                    case MatroskaID.AttachedFile:
                        ReadAttachedFile(child);
                        break;
                    default:
                        break;
                }

                i += child.Size;
            }
        }


        private void ReadAttachedFile(EBMLElement element)
        {
            ulong i = 0;
#pragma warning disable 219 // Assigned, never read
            string file_name = null, file_mime = null, file_desc = null;
            ByteVector file_data = null;
            ulong file_uid = 0;
#pragma warning restore 219

            while (i < element.DataSize)
            {
                EBMLElement child = new EBMLElement(this, element.DataOffset + i);

                MatroskaID matroska_id = (MatroskaID)child.ID;

                switch (matroska_id)
                {
                    case MatroskaID.FileName:
                        file_name = child.ReadString();
                        break;
                    case MatroskaID.FileMimeType:
                        file_mime = child.ReadString();
                        break;
                    case MatroskaID.FileDescription:
                        file_desc = child.ReadString();
                        break;
                    case MatroskaID.FileData:
                        file_data = child.ReadBytes();
                        break;
                    case MatroskaID.FileUID:
                        file_uid = child.ReadULong();
                        break;
                    default:
                        break;
                }

                i += child.Size;
            }

            if (file_mime != null && file_data!=null)
            {
                var attachments = tags.Attachments;

                Array.Resize(ref attachments, tags.Attachments.Length + 1);

                var attach = new Attachment(file_data);
                attach.Filename = file_name;
                attach.Description = file_desc != null ? file_desc : file_name;
                attach.MimeType = file_mime;
                attach.UID = file_uid;

                if (file_mime.StartsWith("image/"))
                {

                    // Set picture type from its name
                    string fname = file_name.ToLower();
                    attach.Type = PictureType.Other;
                    foreach (var ptype in Enum.GetNames(typeof(PictureType)))
                    {
                        if (fname.Contains(ptype.ToLower()))
                        {
                            attach.Type = (PictureType)Enum.Parse(typeof(PictureType), ptype);
                            break;
                        }
                    }

                    if (attach.Type == PictureType.Other && (fname.Contains("cover") || fname.Contains("poster")))
                    {
                        attach.Type = PictureType.FrontCover;
                    }

                }
                else
                {
                    attach.Type = PictureType.NotAPicture;
                }


                attachments[attachments.Length - 1] = attach;
                tags.Attachments = attachments;
            }

        }

        private void WriteAttachments(EBMLElement element)
        {
            ulong i = element.DataOffset;

            foreach (var attach in tags.Attachments)
            {
                // Write AttachedFile content
                if (attach != null && attach.Data != null && attach.Data.Count > 0)
                {
                    // Create new EBML AttachedFile
                    var ebml_attach = new EBMLElement(this, i, MatroskaID.AttachedFile);
                    WriteAttachedFile(ebml_attach, attach);
                    i += ebml_attach.Size;
                }
            }

            // Update Tags EBML data-size
            element.DataSize = i - element.DataOffset;

        }

        private void WriteAttachedFile(EBMLElement element, Attachment attach)
        {
            ulong i = element.DataOffset;

            // Write AttachedFile content

            if (!string.IsNullOrEmpty(attach.Description) && attach.Description != attach.Filename)
            {
                var ebml_obj = new EBMLElement(this, i, MatroskaID.FileDescription, attach.Description);
                i += ebml_obj.Size;
            }

            if (!string.IsNullOrEmpty(attach.Filename))
            {
                var ebml_obj = new EBMLElement(this, i, MatroskaID.FileName, attach.Filename);
                i += ebml_obj.Size;
            }

            if (!string.IsNullOrEmpty(attach.MimeType))
            {
                var ebml_obj = new EBMLElement(this, i, MatroskaID.FileMimeType, attach.MimeType);
                i += ebml_obj.Size;
            }

            if (attach.UID > 0)
            {
                var ebml_obj = new EBMLElement(this, i, MatroskaID.FileUID, attach.UID);
                i += ebml_obj.Size;
            }

            var ebml_data = new EBMLElement(this, i, MatroskaID.FileData, attach.Data);
            i += ebml_data.Size;


            // Update Tag EBML data-size
            element.DataSize = i - element.DataOffset;
        }



        private long ReadWriteSegmentInfo(EBMLElement element)
        {
            long ret = 0;
            ulong i = 0;

            string tag_string = null;
            EBMLElement ebml_string = null;


            while (i < (ulong)((long)element.DataSize + ret))
            {
                EBMLElement child = new EBMLElement(this, element.DataOffset + i);

                MatroskaID matroska_id = (MatroskaID)child.ID;

                switch (matroska_id)
                {
                    case MatroskaID.Duration:
                        duration_unscaled = child.ReadDouble();
                        if (time_scale > 0)
                        {
                            duration = TimeSpan.FromMilliseconds(duration_unscaled * time_scale / 1000000);
                        }
                        break;
                    case MatroskaID.TimeCodeScale:
                        time_scale = child.ReadULong();
                        if (duration_unscaled > 0)
                        {
                            duration = TimeSpan.FromMilliseconds(duration_unscaled * time_scale / 1000000);
                        }
                        break;
                    case MatroskaID.Title:
                        ebml_string = child;
                        tag_string = child.ReadString();
                        break;
                    default:
                        break;
                }

                i += child.Size;
            }

            if (Mode == AccessMode.Write)
            {
                // Write SegmentInfo Title
                string title = tags.Title;
                if (title != null)
                {
                    if (ebml_string == null)
                    {
                        // Create the missing EBML string at the end for the current element
                        ebml_string = new EBMLElement(this, element.Offset + element.Size, MatroskaID.Title, title);
                        ret += (long)ebml_string.Size;
                    }
                    else if (tag_string != title)
                    {
                        // Replace existing string inside the EBML string
                        ret += ebml_string.WriteData(title);
                    }
                }
                else if (ebml_string != null)
                {
                    ret += ebml_string.Remove();
                }

                // Update Element EBML data-size
                ret = element.ResizeData(ret);
            }
            else
            {
                tags.Title = tag_string;
            }

            return ret;
        }

        private void ReadTracks (EBMLElement element)
        {
            ulong i = 0;

            while (i < element.DataSize) {
                EBMLElement child = new EBMLElement (this, element.DataOffset + i);

                MatroskaID matroska_id = (MatroskaID) child.ID;

                switch (matroska_id) {
                    case MatroskaID.TrackEntry:
                        ReadTrackEntry (child);
                        break;
                    default:
                        break;
                }

                i += child.Size;
            }
        }

        private void ReadTrackEntry (EBMLElement element)
        {
            ulong i = 0;

            while (i < element.DataSize) {
                EBMLElement child = new EBMLElement (this, element.DataOffset + i);

                MatroskaID matroska_id = (MatroskaID) child.ID;

                switch (matroska_id) {
                    case MatroskaID.TrackType: {
                            TrackType track_type = (TrackType) child.ReadULong ();

                            switch (track_type) {
                                case TrackType.Video: {
                                        tags.IsVideo = true;
                                        VideoTrack track = new VideoTrack (this, element);

                                        tracks.Add (track);
                                        break;
                                    }
                                case TrackType.Audio: {
                                        AudioTrack track = new AudioTrack (this, element);

                                        tracks.Add (track);
                                        break;
                                    }
                                case TrackType.Subtitle: {
                                        SubtitleTrack track = new SubtitleTrack (this, element);

                                        tracks.Add (track);
                                        break;
                                    }
                                default:
                                    break;
                            }
                            break;
                        }
                    default:
                        break;
                }

                i += child.Size;
            }
        }

        #endregion

    }
}
