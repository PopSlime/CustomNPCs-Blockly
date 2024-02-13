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
	{
		'type': 'MUTATOR_CONTAINER_METHOD_MUTATING',
		'message0': '%{BKY_MUTATOR_CONTAINER_METHOD_MUTATING}',
		'args0': [
			{
				'type': 'field_checkbox',
				'name': 'output',
			},
		],
		'colour': 150,
	},
]);

export const msg = {
	'CNPC_FG_NOPPES_1NPCS_1API_1EVENT_1BLOCKEVENT_3BLOCK': '(%1) event.block',
	'MUTATOR_CONTAINER_METHOD_MUTATING': 'output? %1',
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
Blockly.Extensions.register(
	'INIT_MUTATOR_METHOD_MUTATING',
	function () {
		this.returnTypes_ = this.outputConnection.getCheck();
		this.setOutput(false);
	}
);
Blockly.Extensions.registerMixin(
	'MIXIN_MUTATOR_METHOD_MUTATING',
	{
		captureReturn_: false,
		updateShape_: function () {
			if (this.captureReturn_ == (this.getInput('return') != null)) return;
			if (this.captureReturn_) {
				this.appendDummyInput('return')
					.appendField(Blockly.Msg[this.type.replace('CNPC_M_', 'CNPC_MR_')])
					.appendField(new Blockly.FieldVariable(null, null, this.returnTypes_, this.returnTypes_[0]), 'return');
			}
			else {
				this.removeInput('return');
			}
		}
	}
);
Blockly.Extensions.registerMutator(
	'MUTATOR_METHOD_MUTATING',
	{
		saveExtraState: function () {
			return {
				'captureReturn': this.captureReturn_,
			};
		},
		loadExtraState: function (state) {
			this.captureReturn_ = state['captureReturn'];
			this.updateShape_();
		},
		decompose: function (workspace) {
			const topBlock = workspace.newBlock('MUTATOR_CONTAINER_METHOD_MUTATING');
			topBlock.initSvg();

			topBlock.getField('output').setValue(this.captureReturn_ ? 'TRUE' : 'FALSE');

			return topBlock;
		},
		compose: function (topBlock) {
			this.captureReturn_ = topBlock.getField('output').getValue() == 'TRUE';
			this.updateShape_();
		},
	}
);
