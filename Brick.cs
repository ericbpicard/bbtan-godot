using Godot;

public partial class Brick : StaticBody2D
{
	[Export(PropertyHint.Range, "1,20,1")]
	public int Health { get; set; } = 1;

	private Label _label;
	private Sprite2D _sprite;

	public override void _Ready()
	{
		_label = GetNode<Label>("Label");
		_sprite = GetNode<Sprite2D>("Sprite2D");
		UpdateVisuals();
	}

	public void Initialize(float width, float height)
	{
		_label = GetNode<Label>("Label");
		_sprite = GetNode<Sprite2D>("Sprite2D");

		// Procedural white rectangle fallback (no texture needed)
		if (_sprite.Texture == null)
			CreateProceduralTexture();

		// Resize collision
		var colShape = GetNode<CollisionShape2D>("CollisionShape2D");
		var shape = (RectangleShape2D)colShape.Shape.Duplicate();
		shape.Size = new Vector2(width, height);
		colShape.Shape = shape;
		GD.Print(width);
		GD.Print(height);

		// Scale sprite and label
		float baseWidth = 64f;
		float baseHeight = 32f;
		_sprite.Scale = new Vector2(width / baseWidth, height / baseHeight);
		GD.Print(_sprite.Scale);
		int baseFontSize = 24;  // change this base size if you want bigger/smaller numbers
		_label.AddThemeFontSizeOverride("font_size", (int)(baseFontSize * (width / baseWidth)));
		_label.Scale = new Vector2(1, 1);  // reset scale to 1
		UpdateVisuals();
	}

	private void CreateProceduralTexture()
	{
		var image = Image.CreateEmpty(64, 32, false, Image.Format.Rgba8);
		image.Fill(Colors.White);                    // white base for tinting
		_sprite.Texture = ImageTexture.CreateFromImage(image);
	}

	public void TakeHit(int damage = 1)
	{
		Health -= damage;
		UpdateVisuals();
		if (Health <= 0) QueueFree();
	}

	private void UpdateVisuals()
	{
		if (_label != null)
			_label.Text = Health.ToString();

		if (_sprite != null)
		{
			float t = Mathf.Clamp((Health - 1) / 10f, 0f, 1f);
			_sprite.Modulate = Colors.LimeGreen.Lerp(Colors.Red, t);
		}
	}

}