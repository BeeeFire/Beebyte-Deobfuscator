using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Beebyte_Deobfuscator.Lookup
{
    struct LookupVertex
    {
        public LookupType Type { get; set; }
        public int StaticFieldCount { get; set; }
        public int LiteralFieldCount { get; set; }
        public int GenericFieldCount { get; set; }
        public int PropertyCount { get; set; }
        public bool Nested { get; set; }
    }
    class LookupMatrix
    {
        private readonly List<LookupVertex> _matrix;
        public LookupMatrix()
        {
            _matrix = new List<LookupVertex>();
        }

        public void Insert(LookupVertex vertex)
        {
            _matrix.Add(vertex);
        }

        public void Insert(LookupType item)
        {
            Insert(new LookupVertex()
            {
                Type = item,
                StaticFieldCount = item.Fields.Count(f => f.IsStatic),
                LiteralFieldCount = item.Fields.Count(f => f.IsLiteral),
                GenericFieldCount = item.Fields.Count(f => !f.IsStatic && !f.IsLiteral),
                PropertyCount = item.Properties.Count,
                Nested = item.IsNested
            });
        }

        public List<LookupVertex> GetVertices(LookupVertex vertex)
        {
            int total = vertex.LiteralFieldCount + vertex.GenericFieldCount + vertex.StaticFieldCount + vertex.PropertyCount;
            var t1 = _matrix.Where(
                l => l.StaticFieldCount == vertex.StaticFieldCount
                && l.GenericFieldCount == vertex.GenericFieldCount && l.LiteralFieldCount == vertex.LiteralFieldCount
                && l.PropertyCount == vertex.PropertyCount && l.Type.Namespace == vertex.Type.Namespace
                && l.Type.IsNested == vertex.Nested).ToList();
            if (t1 == null || t1.Count == 0)
            {
                // 5% margin of error
                int marginError = (int)((float)total * 0.1);
                return _matrix.Where(l => (l.StaticFieldCount - marginError <= vertex.StaticFieldCount) && (vertex.StaticFieldCount <= l.StaticFieldCount)
                && ((l.GenericFieldCount - marginError <= vertex.GenericFieldCount) && (vertex.GenericFieldCount <= l.GenericFieldCount))
                && ((l.LiteralFieldCount - marginError <= vertex.LiteralFieldCount) && (vertex.LiteralFieldCount <= l.LiteralFieldCount))
                && ((l.PropertyCount - marginError <= vertex.PropertyCount) && (vertex.PropertyCount <= l.PropertyCount))
                && (l.Type.Namespace == vertex.Type.Namespace || l.Type.Namespace == "")
                && (l.Type.IsNested == vertex.Nested)).ToList();
            }
            return t1;
            //return _matrix.Where(l => l.StaticFieldCount == vertex.StaticFieldCount && l.GenericFieldCount == vertex.GenericFieldCount && l.LiteralFieldCount == vertex.LiteralFieldCount && l.PropertyCount == vertex.PropertyCount && l.Type.Namespace == vertex.Type.Namespace).ToList();
        }

        public List<LookupType> Get(LookupType item)
        {

            LookupVertex tVertex = new LookupVertex()
            {
                Type = item,
                StaticFieldCount = item.Fields.Count(f => f.IsStatic),
                LiteralFieldCount = item.Fields.Count(f => f.IsLiteral),
                GenericFieldCount = item.Fields.Count(f => !f.IsStatic && !f.IsLiteral),
                PropertyCount = item.Properties.Count,
                Nested = item.IsNested
            };

            List<LookupVertex> vertexSet = GetVertices(tVertex);
            List<LookupType> typeList = new List<LookupType>();
            if (vertexSet == null || vertexSet.Count == 0)
            {
                return typeList;
            }

            typeList = vertexSet.Select(x => x.Type).ToList();
            return typeList;
        }
        public int Count()
        {
            return _matrix.Count;
        }
    }
}
