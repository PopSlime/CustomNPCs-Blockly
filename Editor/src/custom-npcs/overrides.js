/**
 * @license
 * Copyright 2024 Cryville
 * SPDX-License-Identifier: Apache-2.0
 */

import * as Blockly from 'blockly/core';

export const blocks = Blockly.common.createBlockDefinitionsFromJsonArray([
	{
		'type': 'CNPC_FG_NOPPES_1NPCS_1API_1EVENT_1BLOCKEVENT_3BLOCK',
		'message0': '%{BKY_CNPC_FG_NOPPES_1NPCS_1API_1EVENT_1BLOCKEVENT_3BLOCK}',
		'args0': [
			{
				'type': 'field_dropdown',
				'name': 'output',
				'options': [
					['%{BKY_CNPC_T_NOPPES_1NPCS_1API_1BLOCK_1IBLOCKSCRIPTED}', 'noppes/npcs/api/block/IBlockScripted'],
					['%{BKY_CNPC_T_NOPPES_1NPCS_1API_1BLOCK_1IBLOCKSCRIPTEDDOOR}', 'noppes/npcs/api/block/IBlockScriptedDoor'],
				],
			},
		],
		'output': ['noppes/npcs/api/block/IBlock', 'Object'],
		'extensions': ['CNPC_VFG_NOPPES_1NPCS_1API_1EVENT_1BLOCKEVENT_3BLOCK'],
		'colour': 30,
	},
]);

export const msg = {
	'CNPC_FG_NOPPES_1NPCS_1API_1EVENT_1BLOCKEVENT_3BLOCK': '(%1) event.block',
};

Blockly.Extensions.register(
	'CNPC_VFG_NOPPES_1NPCS_1API_1EVENT_1BLOCKEVENT_3BLOCK',
	function () {
		var block = this;
		function updateOutput(value) {
			block.setOutput(true, [value, 'noppes/npcs/api/block/IBlock', 'Object']);
		}
		var field = this.getField('output');
		updateOutput(field.getValue());
		field.setValidator(function (option) { updateOutput(option); });
	}
);
