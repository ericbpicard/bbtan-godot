#nullable enable
using Godot;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public partial class Main : Node2D
{
	[Export] private Node2D? _walls;
	[Export] private StaticBody2D? _leftWall;
	[Export] private StaticBody2D? _rightWall;
	[Export] private StaticBody2D? _topWall;
	[Export] public PackedScene? BrickScene { get; set; }
	[Export] public PackedScene? PowerUpScene { get; set; }
	[Export] public float _ballRadius { get; set; } = 12f;
	private int _currentLevel = 1;
	private int _dmg = 1;
	private int _currentRow = 0;
	private int[,] _cells = new int[14, 7];
	private Node2D? _bricksContainer;
	//give walls depth to catch fast objects
	private float padding = 100f;
	// new rows always spawn here
	private const float RowGap = 4f;

	private Cannon? _cannon;

	private DespawnZone? _despawnZone;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var screenSize = GetViewportRect().Size;
		_bricksContainer = GetNode<Node2D>("BricksContainer");
		_cannon = GetNode<Cannon>("Cannon");
		_cannon._ballRadius = _ballRadius;
		_despawnZone = GetNode<DespawnZone>("DespawnZone");
		GD.Print(_ballRadius);
		_despawnZone.SetupZoneDimensions(_ballRadius);

		SetupVerticalWall(_leftWall!, screenSize.Y + padding, new Vector2(0, screenSize.Y / 2));
		SetupVerticalWall(_rightWall!, screenSize.Y + padding, new Vector2(screenSize.X, screenSize.Y / 2));
		SetupHorizontalWall(_topWall!, screenSize.X + padding * 2, new Vector2(screenSize.X / 2, 0));

		CallDeferred(MethodName.AddNewRow);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void SetupVerticalWall(StaticBody2D wall, float fullHeight, Vector2 position)
	{
		//sets up side wall vectors 
		wall.Position = position;
		var shapeNode = wall.GetNode<CollisionShape2D>("CollisionShape2D");
		var segment = shapeNode.Shape as SegmentShape2D ?? new SegmentShape2D();
		shapeNode.Shape = segment;
		var halfHeight = fullHeight / 2;
		segment.A = new Vector2(0, -halfHeight);
		segment.B = new Vector2(0, halfHeight);
		SetupWallPhysicsMaterial(wall);
		SetupCollision(wall);
	}

	private void SetupHorizontalWall(StaticBody2D wall, float fullWidth, Vector2 position)
	{
		//sets up top wall vectors
		wall.Position = position;
		var shapeNode = wall.GetNode<CollisionShape2D>("CollisionShape2D");
		var segment = shapeNode.Shape as SegmentShape2D ?? new SegmentShape2D();
		shapeNode.Shape = segment;
		var halfWidth = fullWidth / 2;
		segment.A = new Vector2(-halfWidth, 0);
		segment.B = new Vector2(halfWidth, 0);
		SetupWallPhysicsMaterial(wall);
		SetupCollision(wall);
	}

	private void SetupWallPhysicsMaterial(StaticBody2D wall)
	{
		// Add physics material for bouncy walls
		var material = new PhysicsMaterial
		{
			Friction = 0f,
			Rough = false,
			Bounce = 1f,
			Absorbent = false
		};
		wall.PhysicsMaterialOverride = material;
	}

	private void SetupCollision(StaticBody2D wall)
	{
		wall.CollisionLayer = 1;  // Walls layer
		wall.CollisionMask = 2;   // No need to collide with anything actively
	}

	public void AddNewRow()
	{
		ResetDamageMult();
		var viewport = GetViewportRect();
		float sideMargin = 4f;
		float availableWidth = viewport.Size.X - (sideMargin * 2);
		float brickWidth = availableWidth / 7f;           // exactly 7 bricks
		float brickHeight = brickWidth * 0.5f;            // change 0.5f for taller/shorter bricks
		float TopRowY = brickHeight + (brickHeight / 2);
		int i;
		//GD.Print(TopRowY);
		//GD.Print(brickHeight);
		// 1. Shift everything down so the new row is at the top
		ShiftBricksDown(brickHeight + RowGap);
		int brickCount = GD.RandRange(1, 3);
		int[] possibleSlots = Enumerable.Range(0, 7).ToArray();
		Random.Shared.Shuffle(possibleSlots);

		float totalWidth = brickCount * brickWidth;

		float startX = sideMargin + (brickWidth / 2);
		for (i = 0; i < brickCount; i++)
		{
			int slot = possibleSlots[i];
			var brick = BrickScene?.Instantiate<Brick>();

			brick?.Initialize(brickWidth - sideMargin, brickHeight);

			brick.GlobalPosition = new Vector2(startX + (slot * brickWidth), TopRowY);
			//GD.Print(brick.Position);
			// health scales with level
			brick.Health = _currentLevel;

			_bricksContainer?.AddChild(brick);
		}
		if (GD.Randf() < 1f)
		{
			int slot = possibleSlots[i + 1];
			var powerup = PowerUpScene?.Instantiate<PowerUp>();
			powerup?.Initialize((PowerUp.PowerUpType)(GD.Randi() % 2), brickWidth - sideMargin, brickHeight);
			_bricksContainer?.AddChild(powerup);
			powerup.GlobalPosition = new Vector2(startX + (slot * brickWidth), TopRowY);
			GD.Print(powerup.CollisionLayer);

		}

		_currentLevel++;
		//_cannon?.IncreaseBallsPerShot();


	}

	// Helper to move all existing bricks down
	private void ShiftBricksDown(float amount)
	{
		foreach (Node child in _bricksContainer.GetChildren())
		{
			if (child is Brick brick)
				brick.Position += new Vector2(0, amount);
			if (child is PowerUp powerUp)
				powerUp.Position += new Vector2(0, amount);
		}
	}

	public void SetDamageMult(int dmg)
	{
		this._dmg*=dmg;
	}

	public void ResetDamageMult()
	{
		this._dmg = 1;
	}
	public int GetDamageMult()
	{
		return this._dmg;
	}
}
