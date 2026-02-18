#nullable enable
using Godot;
using System;

public partial class PowerUp : Area2D
{
	public enum PowerUpType
	{
		AddBall,      // +1 ball/shot (black circle, white "+")
		DoubleDamage// Next balls do 2x dmg (red circle, "2x")
					//more coming, just gonna do 2 for now

	}
	[Export] public PowerUpType Type { get; set; } = PowerUpType.AddBall;
	private Label _label;
	private Sprite2D _sprite;
	private CollisionShape2D _collisionShape;
	private Main? _main;
	private Cannon? _cannon;

	public override void _Ready()
	{
		_main = GetTree().CurrentScene as Main;
		_cannon = _main?.GetNode<Cannon>("Cannon");

		UpdateVisuals();  // Initial (uses Type)
	}

	public void Initialize(PowerUpType type, float width, float height)
	{
		_label = GetNode<Label>("Label");
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		Type = type;
		float diameter = Mathf.Min(width, height) * 0.95f;

		// Circle collision
		var circleShape = new CircleShape2D { Radius = diameter / 2f };
		_collisionShape.Shape = circleShape;

		// Sprite scale
		const int texDiameter = 128;
		_sprite.Scale = new Vector2(diameter / texDiameter, diameter / texDiameter);
		_sprite.Centered = true;

		// Label: Dynamic per type
		UpdateVisuals();
	}

	private void UpdateVisuals()
	{
		CreateProceduralIcon();
		switch (Type)
		{
			case PowerUpType.AddBall:
				_label.Text = "+";
				_label.Modulate = Colors.White;
				break;
			case PowerUpType.DoubleDamage:
				_label.Text = "2x";
				_label.Modulate = Colors.White;
				break;
		}
		// Scale/center label
		float fontScale = _sprite.Scale.X * 1.2f;
		_label.AddThemeFontSizeOverride("font_size", (int)(32 * fontScale));
	}

	private void CreateProceduralIcon()
	{
		const int diameter = 128;
		var image = Image.CreateEmpty(diameter, diameter, false, Image.Format.Rgba8);
		image.Fill(new Color(0, 0, 0, 0));  // Transparent

		Color bgColor;
		switch (Type)
		{
			case PowerUpType.AddBall: bgColor = Colors.Black; break;      // Your screenshot!
			case PowerUpType.DoubleDamage: bgColor = new Color(1f, 0.2f, 0.2f); break;  // Red
			default: bgColor = Colors.Gray; break;
		}

		int radius = diameter / 2;
		// Filled circle BG
		for (int x = 0; x < diameter; x++)
			for (int y = 0; y < diameter; y++)
			{
				float dist = new Vector2(x - radius, y - radius).Length();
				if (dist <= radius - 2)  // Slight inner border
					image.SetPixel(x, y, bgColor);
			}

		var texture = ImageTexture.CreateFromImage(image);
		_sprite.Texture = texture;
	}

	public void Activate()
	{
		switch (Type)
		{
			case PowerUpType.AddBall:
				_cannon?.IncreaseBallsPerShot(1);
				break;
			case PowerUpType.DoubleDamage:
				// Example: Set global damage multiplier (add to Ball.cs or Main)
				_main?.SetDamageMult(2);  // Implement below
				_cannon?.ScaleExistingBalls();
				break;
		}
		QueueFree();  // Destroy
	}

	private void OnBodyEntered(Node2D body)
	{
		GD.Print("Entered");
		if (body is Ball)
		{
			Activate();  // Effect + QueueFree()
		}
	}
}
