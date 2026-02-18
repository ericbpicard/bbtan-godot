#nullable enable
using Godot;
using System;

public partial class Ball : RigidBody2D
{
	private Vector2 screenSize;
	private int _ballDamage = 1;
	public CollisionShape2D collisionShape;
	private float _radius;

	public Ball()
	{
		SetBallPhysics();

	}
	public void Init(float radius)
	{
		_radius = radius;
		CreateBallCollision();
	}
	public override void _Ready()
	{
		screenSize = GetViewportRect().Size;
		ContactMonitor = true;
		MaxContactsReported = 16;
		PhysicsInterpolationMode = PhysicsInterpolationModeEnum.On;
		ContinuousCd = CcdMode.CastRay;
		CreateBallSprite(null);
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void SetBallPhysics()
	{
		GravityScale = 0f;
		LinearDampMode = DampMode.Replace;
		AngularDampMode = DampMode.Replace;
		LinearDamp = 0f;
		AngularDamp = 0f;
		Mass = 1f;
		var material = new PhysicsMaterial
		{
			Friction = 0f,
			Bounce = 1f,
			Rough = false,
			Absorbent = false
		};
		PhysicsMaterialOverride = material;
	}

	public void DisableBallCollision()
	{
		collisionShape.Disabled = true;
	}

	private void CreateBallCollision()
	{
		collisionShape = new CollisionShape2D();
		collisionShape.Shape = new CircleShape2D { Radius = _radius };
		AddChild(collisionShape);

	}

	private void CreateBallSprite(Texture2D? ballTexture)
	{
		var sprite = new Sprite2D();
		sprite.Centered = true;
		Texture2D finishedTexture;
		var diameter = (int)_radius * 2;
		if (ballTexture != null)
		{
			finishedTexture = ballTexture;
			sprite.TextureFilter = CanvasItem.TextureFilterEnum.Linear;  // Smooth scaling
		}
		else
		{
			// Fallback: Procedural white circle (no import needed)
			var image = CreateBallImage(_radius);
			finishedTexture = ImageTexture.CreateFromImage(image);
		}
		var texSize = finishedTexture.GetSize();
		sprite.Texture = finishedTexture;
		sprite.Scale = new Vector2(diameter / texSize.X, diameter / texSize.Y);
		sprite.Modulate = new Color(1, 1, 1, 1);       // White; tint with Colors.Red for fun
		AddChild(sprite);
	}

	public static Image CreateBallImage(float radius)
	{
		var diameter = (int)radius * 2;
		var image = Image.CreateEmpty(diameter, diameter, false, Image.Format.Rgba8);
		for (int x = 0; x < diameter; x++)
		{
			for (int y = 0; y < diameter; y++)
			{
				var pointDistance = new Vector2(x - radius, y - radius).Length();
				if (pointDistance <= radius)
				{
					var alpha = Mathf.Clamp(1.0f - (pointDistance / radius), 0f, 1f);
					//closer to the edge you get, the more transparent the color is
					image.SetPixel(x, y, new Color(1, 1, 1, alpha));
				}

			}
		}
		return image;

	}
	public override void _PhysicsProcess(double delta)
	{
		var pos = GlobalPosition;
		if (pos.Y > screenSize.Y)
		{
			if (LinearVelocity.Y > 0)
				LinearVelocity = new Vector2(0, 0);
		}
	}
	private void OnBodyEntered(Node body)
	{
		if (body is Brick brick)
		{
			brick.TakeHit(_ballDamage);   // damage per hit
		}
	}
	public void SetBallDamage(int damage)
	{
		this._ballDamage = damage;
	}
}

