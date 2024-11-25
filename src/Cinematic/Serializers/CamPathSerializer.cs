using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace UnityExplorer.Serializers
{
    public class CamPathSerializer
    {
        public static string Serialize(List<CatmullRom.CatmullRomPoint> points, float time, float alpha, float tension, bool closePath, string sceneName)
        {
            CamPathSerializeObject serializeObject = new CamPathSerializeObject(points, time, alpha, tension, closePath, sceneName);
            var serializer = new XmlSerializer(typeof(CamPathSerializeObject));
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, serializeObject);
                return writer.ToString();
            }
        }

        public static CamPathSerializeObject Deserialize(string xml)
        {
            var serializer = new XmlSerializer(typeof(CamPathSerializeObject));
            using (var reader = new StringReader(xml))
            {
                return ((CamPathSerializeObject)serializer.Deserialize(reader));
            }
        }
    }

    public struct CamPathSerializeObject
    {
        public CamPathSerializeObject(List<CatmullRom.CatmullRomPoint> points, float time, float alpha, float tension, bool closePath, string sceneName)
        {
            this.points = points;
            this.time = time;
            this.tension = tension;
            this.alpha = alpha;
            this.closePath = closePath;
            this.sceneName = sceneName;
        }

        public readonly List<CatmullRom.CatmullRomPoint> points;
        public readonly float time;
        public readonly float alpha;
        public readonly float tension;
        public readonly bool closePath;
        public readonly string sceneName;
    }
}
