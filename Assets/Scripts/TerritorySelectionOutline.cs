using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Mask the original mesh, then draw offset copies only outside its silhouette.
// This also outlines flat meshes whose normals cannot produce an expanded shell.
[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter), typeof(MeshCollider))]
public class TerritorySelectionOutline : MonoBehaviour
{
    private readonly List<MeshRenderer> copies = new List<MeshRenderer>();
    private Material maskMaterial;
    private Material outlineMaterial;
    private MeshRenderer target;
    private MeshFilter source;
    private bool visible;
    private const int Directions = 16;

    // Called by InfoChecker.Select in InfoChecker.cs. Create the mask and border copies on first use,
    // apply outline color and width, and show and synchronize them.
    public void Show(Color outlineColor, float outlineWidth)
    {
        if (maskMaterial == null)
        {
            Shader shader = Resources.Load<Shader>("TerritoryOutline");
            if (shader == null) return;
            target = GetComponent<MeshRenderer>();
            source = GetComponent<MeshFilter>();
            maskMaterial = new Material(shader) { renderQueue = 3999 };
            maskMaterial.SetInt("_StencilComp", (int)CompareFunction.Always);
            maskMaterial.SetInt("_StencilOp", (int)StencilOp.Replace);
            maskMaterial.SetInt("_ColorMask", 0);
            outlineMaterial = new Material(shader) { renderQueue = 4000 };
            outlineMaterial.SetInt("_StencilComp", (int)CompareFunction.NotEqual);
            outlineMaterial.SetInt("_StencilOp", (int)StencilOp.Keep);
            outlineMaterial.SetInt("_ColorMask", 15);
            CreateCopy("Selection mask", maskMaterial, Vector2.zero);
            for (int i = 0; i < Directions; i++)
            {
                float angle = i * Mathf.PI * 2 / Directions;
                CreateCopy("Selection border", outlineMaterial,
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
            }
        }
        outlineMaterial.SetColor("_Color", outlineColor);
        outlineMaterial.SetFloat("_Width", Mathf.Max(0.001f, outlineWidth));
        SetVisible(true);
        LateUpdate();
    }

    // Called by Show. Create a child mesh renderer with the supplied outline material and shader offset,
    // and track it for visibility and cleanup.
    private void CreateCopy(string objectName, Material material, Vector2 offset)
    {
        var child = new GameObject(objectName);
        child.layer = gameObject.layer;
        child.transform.SetParent(transform, false);
        var filter = child.AddComponent<MeshFilter>();
        filter.sharedMesh = source.sharedMesh;
        var renderer = child.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        var properties = new MaterialPropertyBlock();
        properties.SetVector("_Offset", new Vector4(offset.x, offset.y, 0, 0));
        renderer.SetPropertyBlock(properties);
        copies.Add(renderer);
        SetMaterials(renderer, material);
    }

    // Called by InfoChecker.Collect in InfoChecker.cs. Identify this component's helper transforms so map
    // serialization excludes the mask and border objects.
    public bool OwnsTransform(Transform child)
    {
        foreach (var copy in copies)
            if (copy != null && copy.transform == child) return true;
        return false;
    }

    // Called by CreateCopy and LateUpdate. Assign the helper material to every submesh of the current
    // source mesh.
    private void SetMaterials(MeshRenderer renderer, Material material)
    {
        // Every submesh participates, regardless of the source material setup.
        int count = source.sharedMesh == null ? 1 : Mathf.Max(1, source.sharedMesh.subMeshCount);
        var materials = new Material[count];
        for (int i = 0; i < count; i++) materials[i] = material;
        renderer.sharedMaterials = materials;
    }

    // Called by InfoChecker.Select in InfoChecker.cs, and locally by Show and OnDisable. Set outline
    // visibility while respecting the source renderer's enabled state.
    public void SetVisible(bool value)
    {
        visible = value;
        foreach (var copy in copies)
            if (copy != null) copy.enabled = value && target != null && target.enabled;
    }

    // Unity calls this after frame updates; Show also calls it immediately. Synchronize visible copies
    // with the source mesh, renderer visibility, and rendering layers.
    private void LateUpdate()
    {
        if (!visible || target == null || source == null) return;
        foreach (var copy in copies)
        {
            if (copy == null) continue;
            copy.enabled = target.enabled && source.sharedMesh != null;
            copy.gameObject.layer = gameObject.layer;
            copy.renderingLayerMask = target.renderingLayerMask;
            var filter = copy.GetComponent<MeshFilter>();
            if (filter.sharedMesh != source.sharedMesh)
            {
                filter.sharedMesh = source.sharedMesh;
                SetMaterials(copy, copy == copies[0] ? maskMaterial : outlineMaterial);
            }
        }
    }

    // Unity calls this when the component becomes disabled. Hide all outline helper renderers.
    private void OnDisable() => SetVisible(false);
    // Unity calls this when the component is destroyed. Destroy its helper objects and runtime-created
    // materials.
    private void OnDestroy()
    {
        foreach (var copy in copies) if (copy != null) Destroy(copy.gameObject);
        if (maskMaterial != null) Destroy(maskMaterial);
        if (outlineMaterial != null) Destroy(outlineMaterial);
    }
}
