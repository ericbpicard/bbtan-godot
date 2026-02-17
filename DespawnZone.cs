using Godot;
using System;

public partial class DespawnZone : Area2D
{
	private CollisionShape2D _collisionShape;
	private float _zoneHeight = 50f;  // Fixed height—tweak here
									  // Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void SetupZoneDimensions(float ballRadius)
	{
		_collisionShape = GetNode<CollisionShape2D>("DespawnCollisionShape");
		var viewportSize = GetViewportRect().Size;
		var rectShape = (RectangleShape2D)_collisionShape.Shape;

		// Dynamic: Full width, fixed height
		rectShape.Size = new Vector2(viewportSize.X, _zoneHeight);

		// Reposition to bottom-center (optional—editor pos + offset works too)
		Position = new Vector2(viewportSize.X / 2, viewportSize.Y - ((_zoneHeight / 2) - (ballRadius + 10f)));
	}
}
