/**
 * @license
 * Copyright 2024 Cryville
 * SPDX-License-Identifier: Apache-2.0
 */

import * as Blockly from 'blockly/core';

export const blocks = Blockly.common.createBlockDefinitionsFromJsonArray([
	{
		'type': 'CNPC_E__3ISCANCELABLE',
		'message0': '%{BKY_CNPC_E__3ISCANCELABLE}',
		'output': ['Boolean'],
		'colour': 180,
	},
	{
		'type': 'CNPC_E__3ISCANCELED',
		'message0': '%{BKY_CNPC_E__3ISCANCELED}',
		'output': ['Boolean'],
		'colour': 180,
	},
	{
		'type': 'CNPC_E__3SETCANCELED',
		'message0': '%{BKY_CNPC_E__3SETCANCELED}',
		'args0': [
			{
				'type': 'input_value',
				'name': 'cancel',
				'check': 'Boolean',
			}
		],
		'previousStatement': null,
		'nextStatement': null,
		'colour': 60,
	},
]);
