﻿namespace GMaster.Core.Camera.LumixData
{
    using System.Xml.Serialization;

    [XmlRoot(ElementName = "camrply")]
    public class MenuSetRequestResult : BaseRequestResult
    {
        [XmlElement(ElementName = "menuset")]
        public RawMenuSet MenuSet { get; set; }
    }
}