using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Texture Node")]
    public class TextureNode : PropertyNode, IGeneratesBodyCode, IGeneratesVertexShaderBlock, IGeneratesVertexToFragmentBlock
    {
        protected const string kUVSlotName = "UV";
        protected const string kOutputSlotRGBAName = "RGBA";
        protected const string kOutputSlotRName = "R";
        protected const string kOutputSlotGName = "G";
        protected const string kOutputSlotBName = "B";
        protected const string kOutputSlotAName = "A";

        public const int UvSlotId = 0;
        public const int OutputSlotRgbaId = 1;
        public const int OutputSlotRId =2;
        public const int OutputSlotGId =3;
        public const int OutputSlotBId = 4;
        public const int OutputSlotAId = 5;

        [SerializeField]
        private string m_TextureGuid;

        [SerializeField]
        private TextureType m_TextureType;
        
        public override bool hasPreview { get { return true; } }

#if UNITY_EDITOR
        public Texture2D defaultTexture
        {
            get
            {
                if (string.IsNullOrEmpty(m_TextureGuid))
                    return null;

                var path = AssetDatabase.GUIDToAssetPath(m_TextureGuid);
                if (string.IsNullOrEmpty(path))
                    return null;

                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            set
            {
                var assetPath = AssetDatabase.GetAssetPath(value);
                if (string.IsNullOrEmpty(assetPath))
                    return;

                m_TextureGuid = AssetDatabase.AssetPathToGUID(assetPath);
            }
        }
#else
        public Texture2D defaultTexture
        {
            get
            {
                return Texture2D.whiteTexture;
            }
            set
            {}
        }
#endif

        public TextureType textureType
        {
            get { return m_TextureType; }
            set { m_TextureType = value; }
        }

        public TextureNode()
        {
            name = "Texture";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(OutputSlotRgbaId, kOutputSlotRGBAName, kOutputSlotRGBAName, SlotType.Output, SlotValueType.Vector4, Vector4.zero));
            AddSlot(new MaterialSlot(OutputSlotRId, kOutputSlotRName, kOutputSlotRName, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(OutputSlotGId, kOutputSlotGName, kOutputSlotGName, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(OutputSlotBId, kOutputSlotBName, kOutputSlotBName, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(OutputSlotAId, kOutputSlotAName, kOutputSlotAName, SlotType.Output, SlotValueType.Vector1, Vector4.zero));

            AddSlot(new MaterialSlot(UvSlotId, kUVSlotName, kUVSlotName, SlotType.Input, SlotValueType.Vector2, Vector4.zero));
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected int[] validSlots
        {
            get { return new[] {OutputSlotRgbaId, OutputSlotRId, OutputSlotGId, OutputSlotBId, OutputSlotAId, UvSlotId}; }
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var uvSlot = FindInputSlot<MaterialSlot>(UvSlotId);
            if (uvSlot == null)
                return;

            var uvName = "IN.meshUV0.xy";

            var edges = owner.GetEdges(uvSlot.slotReference).ToList();

            if (edges.Count > 0)
            {
                var edge = edges[0];
                var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(edge.outputSlot.nodeGuid);
                uvName = ShaderGenerator.AdaptNodeOutput(fromNode, edge.outputSlot.slotId, ConcreteSlotValueType.Vector2, true);

            }

            string body = "tex2D (" + propertyName + ", " + uvName + ")";
            if (m_TextureType == TextureType.Bump)
                body = precision + "4(UnpackNormal(" + body + "), 0)";
            visitor.AddShaderChunk("float4 " + GetVariableNameForNode() + " = " + body + ";", true);
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            string slotOutput;
            switch (slotId)
            {
                case OutputSlotRId:
                    slotOutput = ".r";
                    break;
                case OutputSlotGId:
                    slotOutput = ".g";
                    break;
                case OutputSlotBId:
                    slotOutput = ".b";
                    break;
                case OutputSlotAId:
                    slotOutput = ".a";
                    break;
                default:
                    slotOutput = "";
                    break;
            }
            return GetVariableNameForNode() + slotOutput;
        }

        public void GenerateVertexToFragmentBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var uvSlot = FindInputSlot<MaterialSlot>(UvSlotId);
            if (uvSlot == null)
                return;

            var edges = owner.GetEdges(uvSlot.slotReference);
            if (!edges.Any())
                UVNode.StaticGenerateVertexToFragmentBlock(visitor, generationMode);
        }

        public void GenerateVertexShaderBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var uvSlot = FindInputSlot<MaterialSlot>(UvSlotId);
            if (uvSlot == null)
                return;

            var edges = owner.GetEdges(uvSlot.slotReference);
            if (!edges.Any())
                UVNode.GenerateVertexShaderBlock(visitor);
        }

        // Properties
        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            visitor.AddShaderProperty(new TexturePropertyChunk(propertyName, description, defaultTexture, m_TextureType, false, exposed));
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            visitor.AddShaderChunk("sampler2D " + propertyName + ";", true);
        }

    /*
        public override bool DrawSlotDefaultInput(Rect rect, Slot inputSlot)
        {
            var uvSlot = FindInputSlot(kUVSlotName);
            if (uvSlot != inputSlot)
                return base.DrawSlotDefaultInput(rect, inputSlot);


            var rectXmax = rect.xMax;
            rect.x = rectXmax - 70;
            rect.width = 70;

            EditorGUI.DrawRect(rect, new Color(0.0f, 0.0f, 0.0f, 0.7f));
            GUI.Label(rect, "From Mesh");

            return false;
        }
        */

        public override PreviewProperty GetPreviewProperty()
        {
            return new PreviewProperty
            {
                m_Name = propertyName,
                m_PropType = PropertyType.Texture2D,
                m_Texture = defaultTexture
            };
        }
        
        public override PropertyType propertyType { get { return PropertyType.Texture2D; } }
    }
    
}