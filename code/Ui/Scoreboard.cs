﻿using Sandbox;
using Sandbox.UI.Construct;

namespace JetpackGame.UI;

public class Scoreboard : Sandbox.UI.Scoreboard<ScoreboardEntry>
{
	protected override void AddHeader()
	{
		Header = Add.Panel( "header" );
		Header.Add.Label( "player", "name" );
		Header.Add.Label( "kills", "kills" );
		Header.Add.Label( "deaths", "deaths" );
		Header.Add.Label( "ping", "ping" );
	}

	RealTimeSince timeSinceSorted;

	public override void Tick()
	{
		base.Tick();

		if ( timeSinceSorted > 0.1f )
		{
			timeSinceSorted = 0;

			// Sort by number of kills, then number of deaths
			Canvas.SortChildren<ScoreboardEntry>( ( x ) => (-x.Client.GetInt( "kills" ) * 1000) + x.Client.GetInt( "deaths" ) );
		}
	}
}

public class ScoreboardEntry : Sandbox.UI.ScoreboardEntry
{

}
