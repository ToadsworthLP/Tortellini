using Godot;
using System;

public sealed class Constants
{
    public sealed class CollisionLayers {
        public static readonly int GROUND = 0;
        public static readonly int ACTOR = 1;
        public static readonly int PLAYER = 2;
    }

    public sealed class FilePath {
        public static readonly string PLAYER_FORM_SCENES = "res://Tortellini/Player/Forms/";
        public static readonly string PLAYER_FRAMES = "res://Tortellini/Player/Sprites/";
    }
}
