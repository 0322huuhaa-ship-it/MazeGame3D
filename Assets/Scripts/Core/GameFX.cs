using UnityEngine;

// ============================================================================
//  MAZE ADVENTURE 3D — Hiệu ứng hạt (pháo sáng nhỏ khi nhặt đồ / mở cổng...)
// ============================================================================

public static class GameFX
{
    static Material particleMat;

    /// <summary>Vật liệu hạt dùng chung (URP Particles/Unlit).
    /// Nguồn chính là vật liệu giữ chỗ trong Resources để chắc chắn có trong build.</summary>
    public static Material ParticleMaterial
    {
        get
        {
            if (particleMat == null)
            {
                Shader sh = null;
                Material keeper = Resources.Load<Material>("ShaderKeeper_Particles");
                if (keeper != null) sh = keeper.shader;
                if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
                if (sh == null) sh = Shader.Find("Standard");
                particleMat = new Material(sh);
            }
            return particleMat;
        }
    }

    /// <summary>Tạo một vụ nổ pháo hoa nhỏ tại vị trí pos.</summary>
    public static void Burst(Vector3 pos, Color color, int count = 24, float size = 0.08f, float speed = 3f)
    {
        var go = new GameObject("FX_Burst");
        go.transform.position = pos;

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = 0.7f;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = color;
        main.gravityModifier = 0.7f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.12f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = ParticleMaterial;

        Object.Destroy(go, 1.6f);
    }
}
