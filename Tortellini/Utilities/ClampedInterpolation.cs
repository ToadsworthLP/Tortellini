using Godot;

public class ClampedInterpolation
{
    public static float Lerp(float a, float b, float t) {
        if(t >= 1) return b;
        if(t <= 0) return a;
        return a + (b - a) * t;
    }

    public static Vector2 Lerp(Vector2 a, Vector2 b, float t) {
        if(t >= 1) return b;
        if(t <= 0) return a;
        Vector2 result = new Vector2();
        result.x = a.x + (b.x - a.x) * t;
        result.y = a.y + (b.y - a.y) * t;
        return result;
    }

    public static Vector3 Lerp(Vector3 a, Vector3 b, float t) {
        if(t >= 1) return b;
        if(t <= 0) return a;
        Vector3 result = new Vector3();
        result.x = a.x + (b.x - a.x) * t;
        result.y = a.y + (b.y - a.y) * t;
        result.z = a.z + (b.z - a.z) * t;
        return result;
    }
}
