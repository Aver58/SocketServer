// Generated by sprotodump. DO NOT EDIT!
// source: D:\Projects\SparkServer\spark-server\server\Game\Tools\..\\Resource\\RPCProtoSchema\\BattleTaskDispatcher.sproto

using System;
using Sproto;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace NetSprotoType { 
	public class BattleTaskDispatcher_OnBattleRequest : SprotoTypeBase {
		private static int max_field_count = 2;
		
		
		private string _method; // tag 0
		public string method {
			get { return _method; }
			set { base.has_field.set_field (0, true); _method = value; }
		}
		public bool HasMethod {
			get { return base.has_field.has_field (0); }
		}

		private string _param; // tag 1
		public string param {
			get { return _param; }
			set { base.has_field.set_field (1, true); _param = value; }
		}
		public bool HasParam {
			get { return base.has_field.has_field (1); }
		}

		public BattleTaskDispatcher_OnBattleRequest () : base(max_field_count) {}

		public BattleTaskDispatcher_OnBattleRequest (byte[] buffer) : base(max_field_count, buffer) {
			this.decode ();
		}

		protected override void decode () {
			int tag = -1;
			while (-1 != (tag = base.deserialize.read_tag ())) {
				switch (tag) {
				case 0:
					this.method = base.deserialize.read_string ();
					break;
				case 1:
					this.param = base.deserialize.read_string ();
					break;
				default:
					base.deserialize.read_unknow_data ();
					break;
				}
			}
		}

		public override int encode (SprotoStream stream) {
			base.serialize.open (stream);

			if (base.has_field.has_field (0)) {
				base.serialize.write_string (this.method, 0);
			}

			if (base.has_field.has_field (1)) {
				base.serialize.write_string (this.param, 1);
			}

			return base.serialize.close ();
		}
	}


	public class BattleTaskDispatcher_OnBattleRequestResponse : SprotoTypeBase {
		private static int max_field_count = 2;
		
		
		private string _method; // tag 0
		public string method {
			get { return _method; }
			set { base.has_field.set_field (0, true); _method = value; }
		}
		public bool HasMethod {
			get { return base.has_field.has_field (0); }
		}

		private string _param; // tag 1
		public string param {
			get { return _param; }
			set { base.has_field.set_field (1, true); _param = value; }
		}
		public bool HasParam {
			get { return base.has_field.has_field (1); }
		}

		public BattleTaskDispatcher_OnBattleRequestResponse () : base(max_field_count) {}

		public BattleTaskDispatcher_OnBattleRequestResponse (byte[] buffer) : base(max_field_count, buffer) {
			this.decode ();
		}

		protected override void decode () {
			int tag = -1;
			while (-1 != (tag = base.deserialize.read_tag ())) {
				switch (tag) {
				case 0:
					this.method = base.deserialize.read_string ();
					break;
				case 1:
					this.param = base.deserialize.read_string ();
					break;
				default:
					base.deserialize.read_unknow_data ();
					break;
				}
			}
		}

		public override int encode (SprotoStream stream) {
			base.serialize.open (stream);

			if (base.has_field.has_field (0)) {
				base.serialize.write_string (this.method, 0);
			}

			if (base.has_field.has_field (1)) {
				base.serialize.write_string (this.param, 1);
			}

			return base.serialize.close ();
		}
	}


}

