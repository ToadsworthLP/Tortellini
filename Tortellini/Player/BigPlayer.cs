using Godot;
using System;

public class BigPlayer : Player
{
    protected override PlayerCollisionShape GetDefaultCollisionShape() {return PlayerCollisionShape.BIG;}
    
}
