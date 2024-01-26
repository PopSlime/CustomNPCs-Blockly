/**
 * @license
 * Copyright 2024 Cryville
 * SPDX-License-Identifier: Apache-2.0
 */

import { Order } from 'blockly/javascript';

export const forBlock = {
	'CNPC_E__3ISCANCELABLE': function () {
		return [`event.isCancelable()`, Order.MEMBER];
	},
	'CNPC_E__3ISCANCELED': function () {
		return [`event.isCanceled()`, Order.MEMBER];
	},
	'CNPC_E__3SETCANCELED': function (b, g) {
		return `event.setCanceled(${g.valueToCode(b, 'cancel', Order.COMMA)});\n`;
	},
};
