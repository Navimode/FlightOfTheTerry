using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JetpackGame.Player;
using JetpackGame.UI;

namespace JetpackGame;

/// <summary>
/// This is your game class. This is an entity that is created serverside when
/// the game starts, and is replicated to the client. 
/// 
/// You can use this to create things like HUDs and declare which player class
/// to use for spawned players.
/// </summary>
public partial class JetpackGame1 : GameManager
{
	public JetpackGame1()
	{
		if ( Game.IsClient )
		{
			Game.RootPanel = new Hud();
		}
	}

	/// <summary>
	/// A client has joined the server. Make them a pawn to play with
	/// </summary>
	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );

		// Create a pawn for this client to play with
		var pawn = new JetpackPlayer();
		client.Pawn = pawn;
		pawn.Respawn();
	}

	public static void MoveToSpawnPoint( Entity pawn )
	{
		var spawnpoint = All
			.OfType<SpawnPoint>()               // get all SpawnPoint entities
			.OrderBy( x => Guid.NewGuid() )     // order them by random
			.FirstOrDefault();                  // take the first one

		if ( spawnpoint == null )
		{
			Log.Warning( $"Couldn't find spawnpoint for {pawn}!" );
			return;
		}

		var tx = spawnpoint.Transform;
		tx.Position = tx.Position + Vector3.Up * 50.0f; // raise it up
		pawn.Transform = tx;
	}

	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );
	}
}
